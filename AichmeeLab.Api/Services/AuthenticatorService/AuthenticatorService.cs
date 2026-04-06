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

        // 1. REMOVED 'async Task' - This is now a standard blocking method
        public ServiceResponse<string> AuthorizeUser(HttpRequestData req, string keyword)
        {
            string clientIp = GetClientIp(req);

            if (string.IsNullOrEmpty(clientIp))
            {
                return new ServiceResponse<string> { Success = false, Message = "Missing IP." };
            }

            if (string.IsNullOrEmpty(keyword) || keyword != _keyword)
            {
                return new ServiceResponse<string> { Success = false, Message = "Incorrect Key" };
            }

            // 2. SYNC CHECK for existing session
            var sessionCookie = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");
            if (sessionCookie != null && !string.IsNullOrWhiteSpace(sessionCookie.Value))
            {
                // Use .Find().FirstOrDefault() (The Sync version)
                var check = _adminProfiles.Find(a => a.SessionToken == sessionCookie.Value).FirstOrDefault();
                
                if (check != null)
                {
                    if (DoIpAndTokenMatchSync(sessionCookie.Value, check.Ip, clientIp))
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

            // 3. GENERATE TOKEN
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
                // 4. THE SYNC ANCHOR - This blocks the thread until the DB write is 100% confirmed
                _adminProfiles.InsertOne(newAdmin);

                // Small physical sleep to ensure the socket buffer clears before return
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

            // 5. BUILD COOKIE
            string cookieHeader = $"AdminSession={token}; Path=/; HttpOnly; SameSite=Strict; Max-Age=2592000";
            if (_isSecure) cookieHeader += "; Secure";

            return new ServiceResponse<string>
            {
                Data = cookieHeader,
                Success = true,
                Message = "Authorized"
            };
        }

        // Helper for IP extraction to keep main logic clean
        private string GetClientIp(HttpRequestData req)
        {
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
            {
                var firstEntry = forwardedIps.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty;
                return firstEntry.Contains(":") ? firstEntry.Split(':')[0] : firstEntry;
            }
            return _isLocal ? "127.0.0.1" : string.Empty;
        }

        // Synchronous version of the IP Matcher
        private bool DoIpAndTokenMatchSync(string sessionToken, string dbIp, string incomingIP)
        {
            if (!dbIp.Equals(incomingIP))
            {
                // Blocking delete
                _adminProfiles.DeleteOne(a => a.SessionToken == sessionToken);
                return false;
            }
            return true;
        }

        // Keep this async as it's a separate entry point, but it uses Find().FirstOrDefaultAsync() safely
        public async Task<ServiceResponse<bool>> CheckAuthorization(HttpRequestData req)
        {
            var sessionToken = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");
            if (sessionToken == null || string.IsNullOrWhiteSpace(sessionToken.Value))
                return new ServiceResponse<bool> { Success = false, Message = "Failed authentication" };

            var result = await _adminProfiles.Find(a => a.SessionToken == sessionToken.Value).FirstOrDefaultAsync();

            if (result == null) return new ServiceResponse<bool> { Success = false, Message = "Failed authentication" };

            string clientIp = GetClientIp(req);
            
            // Reusing the sync matcher here is fine
            if (!DoIpAndTokenMatchSync(sessionToken.Value, result.Ip, clientIp))
            {
                return new ServiceResponse<bool> { Success = false, Message = "Failed Authentication" };
            }

            return new ServiceResponse<bool> { Data = true, Success = true, Message = "Admin Access" };
        }
    }
}