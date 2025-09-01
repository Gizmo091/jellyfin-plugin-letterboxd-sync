using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using LetterboxdSync;

namespace LetterboxdSync.Tests
{
    public class LetterboxdApiTests
    {
        private readonly LetterboxdApi _api;

        public LetterboxdApiTests()
        {
            _api = new LetterboxdApi();
        }

        [Fact]
        public async Task Authenticate_WithValidCredentials_ShouldAuthenticateSuccessfully()
        {
            // Use real test credentials
            string testUsername = "username_test";
            string testPassword = "r&r1cUUu^7*7";

            // Verify that CSRF is empty before authentication
            Assert.Empty(_api.Csrf);

            // This test should succeed without throwing an exception
            await _api.Authenticate(testUsername, testPassword);
            
            // Verify that authentication set a CSRF token
            Assert.NotEmpty(_api.Csrf);
            
            // Pause to avoid rate limiting errors
            await Task.Delay(1000);
        }


        [Fact]
        public async Task Authenticate_WithInvalidCredentials_ShouldThrowException()
        {
            // Pause before test to avoid rate limiting errors
            await Task.Delay(1000);
            
            string invalidUsername = "invalid_user_12345";
            string invalidPassword = "wrong_password";

            // With invalid credentials, authentication should fail
            var exception = await Assert.ThrowsAsync<Exception>(() => _api.Authenticate(invalidUsername, invalidPassword));
            
            // Display error message for debugging
            // and verify that an exception is thrown (which is already the case if we reach here)
            Assert.NotNull(exception.Message);
            Assert.NotEmpty(exception.Message);
            
            // Pause to avoid rate limiting errors
            await Task.Delay(1000);
        }


        [Fact]
        public async Task SearchFilmByTmdbId_WithValidTmdbId_ShouldReturnFilmResult()
        {
            // Pause before test to avoid rate limiting errors
            await Task.Delay(1500);
            
            int tmdbId = 550; // Fight Club

            // This test makes a real request and should succeed
            var result = await _api.SearchFilmByTmdbId(tmdbId);

            // Verify that the result is correct
            Assert.NotNull(result);
            Assert.NotEmpty(result.filmSlug);
            Assert.NotEmpty(result.filmId);
            Assert.Contains("fight-club", result.filmSlug);
            
            // Pause to avoid rate limiting errors
            await Task.Delay(1000);
        }

        [Fact]
        public async Task SearchFilmByTmdbId_WithValidTmdbId_Incredibles2_ShouldReturnFilmResult()
        {
            // Pause before test to avoid rate limiting errors
            await Task.Delay(1500);
            
            int tmdbId = 260513; // Incredibles 2

            // This test makes a real request and should succeed
            var result2 = await _api.SearchFilmByTmdbId(tmdbId);

            // Verify that the result is correct
            Assert.NotNull(result2);
            Assert.NotEmpty(result2.filmSlug);
            Assert.NotEmpty(result2.filmId);
            Assert.Contains("incredibles-2", result2.filmSlug);
            
            // Pause to avoid rate limiting errors
            await Task.Delay(1000);
        }

        [Fact]
        public async Task SearchFilmByTmdbId_WithInvalidTmdbId_ShouldThrowException()
        {
            int tmdbId = 999999999;
            
            var htmlContent = "<html><body></body></html>";

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent),
                RequestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri("https://letterboxd.com/")
                }
            };

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            await Assert.ThrowsAsync<Exception>(() => _api.SearchFilmByTmdbId(tmdbId));
        }

    }
}