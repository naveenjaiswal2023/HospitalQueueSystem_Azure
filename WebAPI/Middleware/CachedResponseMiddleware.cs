using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HospitalQueueSystem.WebAPI.Middleware
{
    public class CachedResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public CachedResponseMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Get)
            {
                await _next(context);
                return;
            }

            var key = $"CACHE_{context.Request.Path}";
            if (_cache.TryGetValue(key, out string cachedResponse))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cachedResponse);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context);

            memStream.Seek(0, SeekOrigin.Begin);
            var responseText = new StreamReader(memStream).ReadToEnd();

            _cache.Set(key, responseText, TimeSpan.FromSeconds(60));
            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBodyStream);
        }
    }

    public static class CachedResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseCachedResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CachedResponseMiddleware>();
        }
    }
}
