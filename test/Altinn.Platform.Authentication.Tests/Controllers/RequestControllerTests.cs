﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.Authentication.Core.Clients.Interfaces;
using Altinn.Authentication.Tests.Mocks;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using Altinn.Platform.Authentication.Clients.Interfaces;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Core.Extensions;
using Altinn.Platform.Authentication.Core.Models;
using Altinn.Platform.Authentication.Core.Models.SystemUsers;
using Altinn.Platform.Authentication.Services;
using Altinn.Platform.Authentication.Services.Interfaces;
using Altinn.Platform.Authentication.Tests.Fakes;
using Altinn.Platform.Authentication.Tests.Mocks;
using Altinn.Platform.Authentication.Tests.RepositoryDataAccess;
using AltinnCore.Authentication.JwtCookie;
using App.IntegrationTests.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Altinn.Platform.Authentication.Tests.Controllers;
#nullable enable

public class RequestControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    
    private readonly Mock<IUserProfileService> _userProfileService = new();
    private readonly Mock<ISblCookieDecryptionService> _sblCookieDecryptionService = new();

    private readonly Mock<TimeProvider> timeProviderMock = new();
    private readonly Mock<IGuidService> guidService = new();
    private readonly Mock<IEventsQueueClient> _eventQueue = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        bool enableOidc = false;
        bool forceOidc = false;
        string defaultOidc = "altinn";

        string configPath = GetConfigPath();

        WebHostBuilder builder = new();

        builder.ConfigureAppConfiguration((context, conf) =>
        {
            conf.AddJsonFile(configPath);
        });

        var configuration = new ConfigurationBuilder()
          .AddJsonFile(configPath)
          .Build();

        configuration.GetSection("GeneralSettings:EnableOidc").Value = enableOidc.ToString();
        configuration.GetSection("GeneralSettings:ForceOidc").Value = forceOidc.ToString();
        configuration.GetSection("GeneralSettings:DefaultOidcProvider").Value = defaultOidc;

        IConfigurationSection generalSettingSection = configuration.GetSection("GeneralSettings");
        
        services.Configure<GeneralSettings>(generalSettingSection);
        services.AddSingleton<IOrganisationsService, OrganisationsServiceMock>();
        services.AddSingleton<ISigningKeysRetriever, SigningKeysRetrieverStub>();
        services.AddSingleton<IJwtSigningCertificateProvider, JwtSigningCertificateProviderStub>();
        services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
        services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverStub>();
        services.AddSingleton<IEnterpriseUserAuthenticationService, EnterpriseUserAuthenticationServiceMock>();
        services.AddSingleton<IOidcProvider, OidcProviderServiceMock>();
        services.AddSingleton(_eventQueue.Object);
        services.AddSingleton(timeProviderMock.Object);
        services.AddSingleton(guidService.Object);
        services.AddSingleton<IUserProfileService>(_userProfileService.Object);
        services.AddSingleton<ISblCookieDecryptionService>(_sblCookieDecryptionService.Object);
        services.AddSingleton<IPDP, PepWithPDPAuthorizationMock>();
        services.AddSingleton<IPartiesClient, PartiesClientMock>();
        services.AddSingleton<ISystemUserService, SystemUserServiceMock>();    
        services.AddSingleton<ISystemRegisterService, SystemRegisterService>();
        services.AddSingleton<IRequestSystemUser, RequestSystemUserService>();
        SetupDateTimeMock();
        SetupGuidMock();
    }

    [Fact]
    public async Task Request_Create_Success()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();
        string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ] 
        };

        // Arrange
        CreateRequestSystemUser req = new() 
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Created, message.StatusCode);       
        
        CreateRequestSystemUserResponse? res = await message.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.NotNull(res);
        Assert.Equal(req.ExternalRef, res.ExternalRef);
    }

    [Fact]
    public async Task Request_Create_UnAuthorized()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();

        // string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ]
        };

        // Arrange
        CreateRequestSystemUser req = new()
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Unauthorized, message.StatusCode);
    }

    [Fact]
    public async Task Get_Request_ByGuid_Ok()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();
        string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ]
        };

        // Arrange
        CreateRequestSystemUser req = new()
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.Created, message.StatusCode);

        CreateRequestSystemUserResponse? res = await message.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.NotNull(res);
        Assert.Equal(req.ExternalRef, res.ExternalRef);

        //Get by Guid
        Guid testId = res.Id;
        string endpoint2 = $"/authentication/api/v1/systemuser/request/vendor/{testId}";

        HttpResponseMessage message2 = await client.GetAsync(endpoint2);
        string debug = "pause_here";
        Assert.Equal(HttpStatusCode.OK, message2.StatusCode);
        CreateRequestSystemUserResponse? res2 = await message2.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.True(res2 is not null);
        Assert.Equal(testId, res2.Id);
    }

    [Fact]
    public async Task Get_Request_ByExternalRef_Ok()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();
        string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ]
        };

        // Arrange
        CreateRequestSystemUser req = new()
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Created, message.StatusCode);
        CreateRequestSystemUserResponse? res = await message.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.NotNull(res);
        Assert.Equal(req.ExternalRef, res.ExternalRef);
        
        // Get the Request
        string endpoint2 = $"/authentication/api/v1/systemuser/request/vendor/byexternalref/{req.SystemId}/{req.PartyOrgNo}/{req.ExternalRef}";

        HttpResponseMessage message2 = await client.GetAsync(endpoint2);
        Assert.Equal(HttpStatusCode.OK, message2.StatusCode);
        CreateRequestSystemUserResponse? res2 = await message2.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.True(res2 is not null);
        Assert.Equal(req.SystemId + req.PartyOrgNo + req.ExternalRef, res2.SystemId + res2.PartyOrgNo + res2.ExternalRef);
    }

    [Fact]
    public async Task Get_Request_By_Party_RequestId()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();
        string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ]
        };

        // Arrange
        CreateRequestSystemUser req = new()
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Created, message.StatusCode);

        CreateRequestSystemUserResponse? res = await message.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.NotNull(res);
        Assert.Equal(req.ExternalRef, res.ExternalRef);

        // Party Get Request
        HttpClient client2 = CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(1337, null, 3));

        int partyId = 500000;

        string partyEndpoint = $"/authentication/api/v1/systemuser/request/{partyId}/{res.Id}";

        HttpRequestMessage partyReqMessage = new(HttpMethod.Get, partyEndpoint);
        HttpResponseMessage partyResponse = await client2.SendAsync(partyReqMessage, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, partyResponse.StatusCode);

        CreateRequestSystemUserResponse? requestGet = JsonSerializer.Deserialize<CreateRequestSystemUserResponse>(await partyResponse.Content.ReadAsStringAsync());
        Assert.NotNull(requestGet);

        Assert.Equal(res.Id, requestGet.Id);
    }

    [Fact]
    public async Task Apptove_Request_By_RequestId()
    {
        // Create System used for test
        string dataFileName = "Data/SystemRegister/Json/SystemRegister.json";
        HttpResponseMessage response = await CreateSystemRegister(dataFileName);

        HttpClient client = CreateClient();
        string token = AddTestTokenToClient(client);
        string endpoint = $"/authentication/api/v1/systemuser/request/vendor";

        Right right = new()
        {
            Resource =
            [
                new AttributePair()
                {
                    Id = "urn:altinn:resource",
                    Value = "ske-krav-og-betalinger"
                }
            ]
        };

        // Arrange
        CreateRequestSystemUser req = new()
        {
            ExternalRef = "external",
            SystemId = "the_matrix",
            PartyOrgNo = "910493353",
            Rights = [right]
        };

        HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(req)
        };
        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.Created, message.StatusCode);

        CreateRequestSystemUserResponse? res = await message.Content.ReadFromJsonAsync<CreateRequestSystemUserResponse>();
        Assert.NotNull(res);
        Assert.Equal(req.ExternalRef, res.ExternalRef);

        // Party Get Request
        HttpClient client2 = CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(1337, null, 3));

        int partyId = 500000;

        string partyEndpoint = $"/authentication/api/v1/systemuser/request/{partyId}/{res.Id}";

        HttpRequestMessage partyReqMessage = new(HttpMethod.Get, partyEndpoint);
        HttpResponseMessage partyResponse = await client2.SendAsync(partyReqMessage, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, partyResponse.StatusCode);

        CreateRequestSystemUserResponse? requestGet = JsonSerializer.Deserialize<CreateRequestSystemUserResponse>(await partyResponse.Content.ReadAsStringAsync());

        //string approveEndpoint = $"/authentication/api/v1/systemuser/request/{partyId}/{res.Id}/approve";
        //HttpRequestMessage approveRequestMessage = new(HttpMethod.Post, approveEndpoint);
        //HttpResponseMessage approveResponseMessage = await client2.SendAsync(approveRequestMessage, HttpCompletionOption.ResponseHeadersRead);
        //Assert.Equal(HttpStatusCode.OK, approveResponseMessage.StatusCode);
    }

    private void SetupDateTimeMock()
    {
        timeProviderMock.Setup(x => x.GetUtcNow()).Returns(new DateTimeOffset(2018, 05, 15, 02, 05, 00, TimeSpan.Zero));
    }

    private void SetupGuidMock()
    {
        guidService.Setup(q => q.NewGuid()).Returns("eaec330c-1e2d-4acb-8975-5f3eba12b2fb");
    }

    private static string GetConfigPath()
    {
        string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AuthenticationControllerTests).Assembly.Location).LocalPath);
        return Path.Combine(unitTestFolder, $"../../../appsettings.json");
    }

    private async Task<HttpResponseMessage> CreateSystemRegister(HttpClient client, string token)
    {
        string data = File.ReadAllText("Data/SystemRegister/Json/SystemRegister.json");
        JsonContent content = JsonContent.Create(data);
        var res = await client.PostAsync(token, $"/authentication/api/v1/systemregister/system/", content);
        return res;
    }

    private static string AddTestTokenToClient(HttpClient client)
    {
        string[] prefixes = ["altinn", "digdir"];
        string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:authentication/systemregister.admin", prefixes);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return token;
    }

    private async Task<HttpResponseMessage> CreateSystemRegister(string dataFileName)
    {
        HttpClient client = CreateClient();
        string[] prefixes = { "altinn", "digdir" };
        string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:authentication/systemregister.admin", prefixes);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Stream dataStream = File.OpenRead(dataFileName);
        StreamContent content = new StreamContent(dataStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpRequestMessage request = new(HttpMethod.Post, $"/authentication/api/v1/systemregister/system/");
        request.Content = content;
        HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        return response;
    }
}
