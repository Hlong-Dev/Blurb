﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DoAnCoSo2.Models;
using DoAnCoSo2.Repositories;
using DoAnCoSo2.Helpers;
using DoAnCoSo2.Data;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IBlogRepository _blogRepo;
        private readonly BookStoreContext _context;
        public class SaveBlogRequest
        {
            public string Slug { get; set; }
        }
        public class UnsaveBlogRequest
        {
            public string Slug { get; set; }
        }

        public ProductsController(IBlogRepository repo, BookStoreContext context)
        {
            _blogRepo = repo;
            _context = context;
        }

        [HttpGet]
      
        public async Task<IActionResult> GetAllBlogs()
        {
            try
            {
                return Ok(await _blogRepo.GetAllBlogsAsync());
            }
            catch
            {
                return BadRequest();
            }
        }
        [HttpGet("private/{userId}")]
        public async Task<IActionResult> GetPrivateBlogs(string userId)
        {
            try
            {
                return Ok(await _blogRepo.GetAllPrivateBlogsByUserAsync(userId));
            }
            catch
            {
                return BadRequest();
            }
        }
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBlogById(string slug)
        {
            var blog = await _blogRepo.GetBlogAsync(slug);
            if (blog == null)
            {
                return NotFound();
            }

            // Gọi phương thức từ repo để cập nhật số lượt xem của bài viết
            await _blogRepo.UpdateViewCountAsync(blog.Slug);

            return Ok(blog);
        }

        [HttpPost]
        public async Task<IActionResult> AddNewBlog(BlogModel model)
        {
            try
            {
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

                // Kiểm tra xem slug đã tồn tại trong cơ sở dữ liệu hay chưa
                var slugExists = await _blogRepo.IsSlugExists(model.Slug);
                if (slugExists)
                {
                    // Nếu slug đã tồn tại, thực hiện một biện pháp để tạo ra một slug mới
                    model.Slug = GenerateUniqueSlug(model.Slug);
                }

                // Tạo một đối tượng Blog mới
                var newBlog = new Blog
                {
                    Title = model.Title,
                    Content = model.Content,
                    Id = model.Id,
                    UserName = model.UserName,
                    CreatedAt = currentDateTime,
                    ImageUrl = model.ImageUrl,
                    Slug = model.Slug,
                    Description = model.Description,
                    AvatarUrl = model.AvatarUrl,
                    FirstName = model.FirstName,
                    CategorySlug = model.CategorySlug,
                    IsPublic = model.IsPublic, // Đảm bảo giá trị `isPublic` được gán đúng
                    ViewCount = model.ViewCount // Đảm bảo viewCount cũng được gán đúng
                };

                // Thêm blog mới vào cơ sở dữ liệu Neo4j và lấy về slug
                var newSlug = await _blogRepo.AddBlogAsync(newBlog, model.Id, model.CategorySlug);

                // Lấy blog mới đã được thêm vào từ cơ sở dữ liệu
                var blog = await _blogRepo.GetBlogAsync(newSlug);

                return blog == null ? NotFound() : Ok(blog);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi và ghi log
                Console.WriteLine($"Error adding new blog: {ex.Message}");
                return BadRequest("Error adding new blog.");
            }
        }

        private string GenerateUniqueSlug(string slug)
        {
            // Tạo một slug mới không trùng lặp, ví dụ: thêm số vào cuối slug
            var uniqueSlug = $"{slug}-{DateTime.Now.Ticks}";

            return uniqueSlug;
        }


        [HttpPost("upload")]
        
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File is empty");

                // Save the uploaded file to a temporary location
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Upload the image to Imgur
                var imgUrl = await _blogRepo.UploadImageAsync(file);

                // Return the URL of the uploaded image
                return Ok(new { url = imgUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{slug}")]
      
        public async Task<IActionResult> UpdateBlog(string slug, [FromBody] BlogModel model)
        {
            if (slug != model.Slug)
            {
                return NotFound();
            }
            await _blogRepo.UpdateBlogAsync(slug, model);
            return Ok();
        }

        [HttpDelete("{slug}")]
      
        public async Task<IActionResult> DeleteBlog([FromRoute] string slug)
        {
            await _blogRepo.DeleteBlogAsync(slug);
            return Ok();
        }
        [HttpPost("saved")]
        public async Task<IActionResult> SaveOrUnsaveBlog([FromBody] SaveBlogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isBlogSaved = await _blogRepo.IsBlogSavedAsync(userId, request.Slug);

            if (isBlogSaved)
            {
                // Nếu bài viết đã được lưu, thực hiện hành động bỏ lưu
                await _blogRepo.UnsaveBlogAsync(userId, request.Slug);
                return Ok("Blog unsaved successfully!");
            }
            else
            {
                // Nếu bài viết chưa được lưu, thực hiện hành động lưu
                await _blogRepo.SaveBlogAsync(userId, request.Slug);
                return Ok("Blog saved successfully!");
            }
        }
        [HttpGet("saved")]
        [Authorize] // Chỉ người dùng đã đăng nhập mới có thể xem lại các bài viết đã lưu
        public async Task<IActionResult> GetSavedBlogs()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Gọi phương thức từ repo để lấy danh sách các bài viết đã lưu
                var savedBlogs = await _blogRepo.GetSavedBlogsAsync(userId);

                return Ok(savedBlogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("noti/{userId}")]
        public async Task<IEnumerable<Notification>> GetUserNotifications(string userId)
        {
            return await _blogRepo.GetNotificationsForUserAsync(userId);
        }
        [HttpPost("{slug}/comments")]
        public async Task<IActionResult> AddComment(string slug, [FromBody] CommentModel model)
        {
            try
            {
                model.BlogSlug = slug;
                await _blogRepo.AddCommentToBlogAsync(model);
                return Ok("Comment added successfully!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("{slug}/comments")]
        public async Task<IActionResult> GetComments(string slug)
        {
            try
            {
                var comments = await _blogRepo.GetCommentsForBlogAsync(slug);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpDelete("{slug}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(string slug, int commentId)
        {
            try
            {
                var blog = await _blogRepo.GetBlogAsync(slug);
                if (blog == null)
                {
                    return NotFound("Blog not found");
                }

                await _blogRepo.DeleteCommentAsync(commentId);

                return Ok("Comment deleted successfully!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularBlogs(int count = 5)
        {
            try
            {
                var popularBlogs = await _blogRepo.GetPopularBlogsAsync(count);
                return Ok(popularBlogs);
            }
            catch
            {
                return BadRequest();
            }
        }
        [HttpGet("followed/{userId}")]
   
        public async Task<IActionResult> GetFollowedUsersBlogs(string userId)
        {
            try
            {
                var blogs = await _blogRepo.GetFollowedUsersBlogsAsync(userId);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchBlogs([FromQuery] string keyword)
        {
            try
            {
                var blogs = await _blogRepo.SearchBlogsAsync(keyword);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}