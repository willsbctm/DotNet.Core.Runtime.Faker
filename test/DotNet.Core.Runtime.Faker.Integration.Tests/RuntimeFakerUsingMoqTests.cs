using DotNet.Core.Runtime.Faker.WebApi.Sample;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Core.Runtime.Faker;
using DotNet.Core.Runtime.Faker.Moq;
using System.Net;

namespace DotNet.Core.Faker.Runtime.Integration.Tests
{
    public class RuntimeFakerUsingMoqTests
    {
        private DateTime registeredValue;
        private static WebApplicationFactory<Program> factory;
        private static IServiceProvider serviceProvider;
        private HttpClient cliente;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            registeredValue = new Bogus.Faker().Date.Future();

            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddServiceWithFaker<Clock>(mock => mock.Setup(x => x.Now()).Returns(registeredValue));
                }));
            serviceProvider = factory.Services;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => factory.Dispose();

        [SetUp]
        public void Setup() => cliente = factory.CreateClient();

        [TearDown]
        public void TearDown()
        {
            cliente.Dispose();
            serviceProvider.ResetAllChanges();
        }

        [Test]
        public async Task ShouldGetRegisteredValueForClock()
        {
            var result = await cliente.GetAsync("time");
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await result.Content.ReadAsStringAsync();

            content.Should().Be($"{registeredValue:yyyy-MM-ddThh:mm:ss}");
        }

        [Test]
        public async Task ShouldChangeClockImplementation()
        {
            var now = new Bogus.Faker().Date.Past();
            serviceProvider.Change<Clock>(mock => mock.Setup(x => x.Now()).Returns(now));

            var result = await cliente.GetAsync("time");
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await result.Content.ReadAsStringAsync();

            content.Should().Be($"{now:yyyy-MM-ddThh:mm:ss}");
        }

        [Test]
        public async Task ShouldCleanChangesAndRestoreRegisteredValue()
        {
            serviceProvider.Change<Clock>(mock => mock.Setup(x => x.Now()).Returns(new Bogus.Faker().Date.Past()));
            serviceProvider.ResetAllChanges();

            var result = await cliente.GetAsync("time");
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await result.Content.ReadAsStringAsync();

            content.Should().Be($"{registeredValue:yyyy-MM-ddThh:mm:ss}");
        }
    }
}