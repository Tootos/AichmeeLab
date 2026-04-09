using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net;

namespace AichmeeLab.Api.Services.AuthenticatorService
{
    class AuthenticatorService : IAuthenticatorService
    {
        readonly IMongoCollection<AdminProfile> _adminProfiles;
        readonly bool _isLocal;
        readonly string _keyword;
        readonly bool _isSecure;

        public AuthenticatorService(IMongoClient mongoClient, IOptions<MonitorDbSettings> options, IConfiguration config)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _adminProfiles = database.GetCollection<AdminProfile>(settings.AdminsCollectionName);

            _keyword = config["AuthorizationKeyword"] ?? "default_key";
            _isSecure = config.GetValue<bool>("UseSecure", true);
            _isLocal = config.GetValue<bool>("IsLocal", false);
        }

        // Compromise
        public ServiceResponse<string> AuthorizeUser(HttpRequestData req, string keyword)
        {
            // 1. Fetch Ip from request 
            string clientIp = GetClientIp(req);

            if (string.IsNullOrEmpty(clientIp))
            {
                return new ServiceResponse<string> { Success = false, Message = "Missing IP." };
            }

            if (string.IsNullOrEmpty(keyword) || keyword != _keyword)
            {
                return new ServiceResponse<string> { Success = false, Message = "Incorrect Key" };
            }

            // 2. CHECK for existing session
            var sessionCookie = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");
            if (sessionCookie != null && !string.IsNullOrWhiteSpace(sessionCookie.Value))
            {
                var check = _adminProfiles.Find(a => a.SessionToken == sessionCookie.Value).FirstOrDefault();

                if (check != null)
                {

                    if (check.Ip.Equals(clientIp))
                    {
                        return new ServiceResponse<string>
                        {
                            Data = "SESSION_VALID",
                            Success = true,
                            Message = "Access confirmed via existing session."
                        };

                    }
                    else
                    {
                        return new ServiceResponse<string> { Success = false, Message = "Invalid Session" };
                    }
                }
            }
            // 3. Generate a new Session Token
            string token = Guid.NewGuid().ToString();

            var newAdmin = new AdminProfile
            {
                SessionToken = token,
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                Ip = clientIp
            };

            try
            {
                // 4. Insert new entry
                // Q: Why is this a synchronous call?
                // A: An issue with the isolated worker in the specific method forced me,\ 
                //    into this compromise. In the future it must be asynchronous
                _adminProfiles.InsertOne(newAdmin);

                
                System.Threading.Thread.Sleep(150);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = $"Database Write Failed: {ex.Message}",
                    Data = ex.StackTrace
                };
            }

            // 5. Cookie baked, deliver it to the Client
            string cookieHeader = $"AdminSession={token}; Path=/; HttpOnly; SameSite=Strict; Max-Age=2592000";
            if (_isSecure) cookieHeader += "; Secure";

            return new ServiceResponse<string>
            {
                Data = cookieHeader,
                Success = true,
                Message = "Authorized"
            };
        }


        //  IP extraction to keep main logic clean
        private string GetClientIp(HttpRequestData req)
        {
            // X-Forwarded-For contains Ip information in Azure 
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
            {
                var firstEntry = forwardedIps.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty;
                return firstEntry.Contains(":") ? firstEntry.Split(':')[0] : firstEntry;
            }
            return _isLocal ? "127.0.0.1" : string.Empty;
        }

        // Check session 
        public async Task<ServiceResponse<bool>> CheckAuthorization(HttpRequestData req)
        {
            var sessionToken = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");
            if (sessionToken == null || string.IsNullOrWhiteSpace(sessionToken.Value))
                return new ServiceResponse<bool> { Success = false, Message = "Failed authentication" };

            var result = await _adminProfiles.Find(a => a.SessionToken == sessionToken.Value).FirstOrDefaultAsync();

            if (result == null) return new ServiceResponse<bool> { Success = false, Message = "Failed authentication" };

            string clientIp = GetClientIp(req);

            // Check if existing IPs match
            // If the Ips don't match delete the entry with the token
            if (!result.Ip.Equals(clientIp))
            {
                await _adminProfiles.DeleteOneAsync(a => a.SessionToken == sessionToken.Value);
                return new ServiceResponse<bool> { Success = false, Message = "Failed Authentication" };
            }

            return new ServiceResponse<bool> { Data = true, Success = true, Message = "Admin Access" };
        }
    }
}