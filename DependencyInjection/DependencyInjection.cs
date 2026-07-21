using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Abstractions;
using SystemContext.Abstractions;
using SystemContext.Infrastructure;

namespace SystemContext.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSystemContext(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            // Đăng ký UserContext là implementation gốc theo scope của từng HTTP request.
            // Nghĩa là mỗi request vào hệ thống sẽ có đúng 1 instance UserContext riêng,
            // instance này chịu trách nhiệm đọc thông tin user hiện tại từ HttpContext.User.Claims.
            services.AddScoped<UserContext>();

            // Khi một class nào đó cần IUserContext,
            // DI sẽ không tạo object mới mà trả lại chính instance UserContext đã tạo ở trên.
            // Mục đích là để toàn bộ request dùng chung một nguồn user context thống nhất.
            services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());

            // Khi một class khác, ví dụ BaseDbContext, chỉ cần một abstraction nhỏ hơn là
            // ICurrentUserProvider để lấy CurrentUserId phục vụ audit,
            // DI cũng vẫn trả lại đúng instance UserContext đó.
            // Như vậy IUserContext và ICurrentUserProvider chỉ là 2 interface khác nhau,
            // nhưng cùng trỏ tới một object UserContext duy nhất trong request hiện tại.
            services.AddScoped<ICurrentUserProvider>(sp => sp.GetRequiredService<UserContext>());

            return services;
        }
    }
}
