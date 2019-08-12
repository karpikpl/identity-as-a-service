// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Specialized;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace IdentityServer
{
    /// <summary>
    /// Return Url parser - see: https://github.com/IdentityServer/IdentityServer4/blob/63a50d7838af25896fbf836ea4e4f37b5e179cd8/src/Services/Default/OidcReturnUrlParser.cs
    /// </summary>
    public class ReturnUrlParser : IReturnUrlParser
    {
        private readonly IAuthorizeRequestValidator validator;
        private readonly IUserSession userSession;
        private readonly ILogger<ReturnUrlParser> logger;

        public ReturnUrlParser(
            IAuthorizeRequestValidator validator,
            IUserSession userSession,
            ILogger<ReturnUrlParser> logger)
        {
            this.validator = validator;
            this.userSession = userSession;
            this.logger = logger;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            // be less restrictive to allow workign with SPA
            return !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("http");
        }

        /// <summary>
        /// parse authorize request context 
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
		public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
        {
            if (IsValidReturnUrl(returnUrl))
            {
                var parameters = ReadQueryStringAsNameValueCollection(returnUrl);
                var user = await this.userSession.GetUserAsync();
                var result = await this.validator.ValidateAsync(parameters, user);
                if (!result.IsError)
                {
                    this.logger.LogTrace("AuthorizationRequest being returned");
                    var req = new AuthorizationRequest()
                    {

                        ClientId = result.ValidatedRequest.ClientId,
                        RedirectUri = result.ValidatedRequest.RedirectUri,
                        DisplayMode = result.ValidatedRequest.DisplayMode,
                        UiLocales = result.ValidatedRequest.UiLocales,
                        IdP = result.ValidatedRequest.GetIdP(),
                        Tenant = result.ValidatedRequest.GetTenant(),
                        LoginHint = result.ValidatedRequest.LoginHint,
                        PromptMode = result.ValidatedRequest.PromptMode,
                        AcrValues = result.ValidatedRequest.GetAcrValues(),
                        ScopesRequested = result.ValidatedRequest.RequestedScopes
                    };

                    req.Parameters.Add(result.ValidatedRequest.Raw);

                    return req;
                }
            }

            return null;
        }

        private static NameValueCollection ReadQueryStringAsNameValueCollection(string url)
        {
            if (url != null)
            {
                var idx = url.IndexOf('?');
                if (idx >= 0)
                {
                    url = url.Substring(idx + 1);
                }
                var query = QueryHelpers.ParseNullableQuery(url);
                if (query != null)
                {
                    return query.AsNameValueCollection();
                }
            }

            return new NameValueCollection();
        }
    }
}
