﻿using iSecureGateway_Suprema.Contexts.Handlers;
using iSecureGateway_Suprema.Interfaces;
using iSecureGateway_Suprema.Mappers;
using iSecureGateway_Suprema.Services;

namespace iSecureGateway_Suprema.Commons.Config
{
    public static class DependencyInjector
    {
        public static IServiceCollection AddServiceDependencyInjection(this IServiceCollection services)
        {
            services.AddSingleton<PostMapper>();
            services.AddSingleton<PostApply>();

            services.AddSingleton<AuthorMapper>();
            services.AddSingleton<AuthorApply>();

            services.AddSingleton<CommonApply>();

            services.AddSingleton<AccessGroupContextHandler>();
            services.AddSingleton<AccessLevelContextHandler>();
            services.AddSingleton<AccessScheduleContextHandler>();
            services.AddSingleton<PostContextHandler>();
            services.AddSingleton<AuthorContextHandler>();

            services.AddTransient<IAccessGroupService, AccessGroupService>();
            services.AddTransient<IAccessLevelService, AccessLevelService>();
            services.AddTransient<IAccessScheduleService, AccessScheduleService>();
            services.AddTransient<IPostService, PostService>();
            services.AddTransient<IAuthorService, AuthorService>();

            return services;
        }
    }
}
