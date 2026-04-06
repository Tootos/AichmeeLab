using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        public async Task<ServiceResponse<string>> AuthorizeUser(HttpRequestData req, string keyword)
        {

            // 1. Grab the IP (Isolated headers are slightly different)
            //This only works when the app is hosted in Azure
            string clientIp = string.Empty;
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
            {
                // 1.1. Get the first entry in the chain
                var firstEntry = forwardedIps.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty;

                // 1.2 Strip the port
                if (firstEntry.Contains(":"))
                    clientIp = firstEntry.Split(':').FirstOrDefault() ?? firstEntry;
                else
                    clientIp = firstEntry;

            }
            else if (_isLocal) //For local development
                clientIp = "127.0.0.1";


            // 2. Initial Guard Clause
            if (string.IsNullOrEmpty(clientIp))
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Data = string.Empty,
                    Message = "Missing IP."
                };
            }

            // 3. Check Keyword
            if (string.IsNullOrEmpty(keyword) || keyword != _keyword)
            {
                // TODO: Update attempts in your Security DB
                return new ServiceResponse<string>
                {
                    Success = false,
                    Data = string.Empty,
                    Message = "Incorrect Key"
                };
            }

            // 4. Check if the header already has a token stored in the DB
            var sessionToken = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");

            if (sessionToken != null && !string.IsNullOrWhiteSpace(sessionToken.Value))
            {
                var check = await _adminProfiles.Find(a => a.SessionToken.Equals(sessionToken.Value)).FirstOrDefaultAsync();
                // If token exists in the DB compare the incoming Ip to the DB Ip
                if (check != null)
                    if (await DoIpAndTokenMatch(sessionToken.Value, check.Ip, clientIp))
                        //If the Ips match then there is an entry for the user 
                        //And we make no changes to the header data
                        return new ServiceResponse<string>
                        {
                            Data = "SESSION_VALID", // Just a status string
                            Success = true,
                            Message = "Access confirmed via existing session."
                        };
                    else
                        //Else we abandon the process IP
                        return new ServiceResponse<string>
                        {
                            Success = false,
                            Data = string.Empty,
                            Message = "Invalid Session"
                        };

            }


            // 5. All checks passed! Create entry in Admin list
            var newAdmin = new AdminProfile
            {
                SessionToken = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                Ip = clientIp
            };

            try
            {
                await _adminProfiles.InsertOneAsync(new AdminProfile { 
                    SessionToken = Guid.NewGuid().ToString(),
                    ExpirationDate = DateTime.UtcNow.AddDays(30), 
                    CreatedAt = DateTime.UtcNow, 
                    Ip = clientIp });

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _adminProfiles.InsertOneAsync(newAdmin, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                // If it fails, we NEED to know why in the browser response
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = $"Database Write Failed: {ex.Message}",
                    Data = ex.StackTrace // Temporarily keep this for debugging
                };
            }


            // 6. We create a special cookie object and add it to the response collection

            string cookieHeader = $"AdminSession={newAdmin.SessionToken}; Path=/; HttpOnly; SameSite=Strict; Max-Age=2592000";

            if (_isSecure) cookieHeader += "; Secure";

            // 7. Return the Cookie string
            return new ServiceResponse<string>
            {
                Data = cookieHeader,
                Success = true,
                Message = "Authorized"
            };
        }

        public async Task<ServiceResponse<bool>> CheckAuthorization(HttpRequestData req)
        {
            var sessionToken = req.Cookies.FirstOrDefault(c => c.Name == "AdminSession");
            // 1. Check if there is a token in the request
            if (sessionToken == null
            || string.IsNullOrWhiteSpace(sessionToken.Value))
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "Failed authentication"
                };



            // 2. Compare the token with entries in the DB 
            var result = await _adminProfiles
            .Find(a => a.SessionToken == sessionToken.Value)
            .FirstOrDefaultAsync();

            if (result == null) return new ServiceResponse<bool>
            {
                Data = false,
                Success = false,
                Message = "Failed authentication"
            };

            // 3. Check if the cookie matches the clients IP
            string clientIp = string.Empty;
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
                clientIp = forwardedIps.FirstOrDefault() ?? string.Empty;
            else if (_isLocal) //For local development
                clientIp = "127.0.0.1";

            if (!await DoIpAndTokenMatch(sessionToken.Value, result.Ip, clientIp))
            {

                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "Failed Authentication"
                };
            }

            return new ServiceResponse<bool>
            {
                Data = true,
                Success = true,
                Message = "Admin Access"
            };

        }


        async Task<bool> DoIpAndTokenMatch(string sessionToken, string dbIp, string incomingIP)
        {
            if (!dbIp.Equals(incomingIP))
            {
                // This could be someone stealing a cookie! 
                // Force a delete of the old session and deny access.
                await _adminProfiles.DeleteOneAsync(a => a.SessionToken == sessionToken);
                //Add this IP to a Blacklist
                return false;
            }

            return true;
        }
    }
}