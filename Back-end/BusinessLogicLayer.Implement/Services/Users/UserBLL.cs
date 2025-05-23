﻿using AutoMapper;
using Business_Logic_Layer.Services_Interface.Users;
using BusinessLogicLayer.Implement.CustomExceptions;
using BusinessLogicLayer.Implement.Microservices.Cloudinaries;
using BusinessLogicLayer.Interface.Services_Interface.JWTs;
using BusinessLogicLayer.ModelView.Service_Model_Views.Authentication.Response;
using BusinessLogicLayer.ModelView.Service_Model_Views.Users.Request;
using BusinessLogicLayer.ModelView.Service_Model_Views.Users.Response;
using CloudinaryDotNet.Actions;
using DataAccessLayer.Interface.MongoDB.UOW;
using DataAccessLayer.Repository.Entities;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using SetupLayer.Enum.Microservices.Cloudinary;
using SetupLayer.Enum.Services.User;
using System.Security.Claims;
using Utility.Coding;

namespace BusinessLogicLayer.Implement.Services.Users
{
    public class UserBLL(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, CloudinaryService cloudinaryService, IJwtBLL jwtBLL) : IUser
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly CloudinaryService _cloudinaryService = cloudinaryService;
        private readonly IJwtBLL _jwtBLL = jwtBLL;

        public async Task<IEnumerable<UserResponseModel>> GetAllUsersAsync(string? displayName, UserGender? gender, string? email, bool isCache = false)
        {
            // Khởi tạo bộ lọc (trống ban đầu)
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Empty;

            // Xây dựng cacheKey dựa trên các bộ lọc
            string cacheKey = "users";

            // Kiểm tra và thêm điều kiện lọc cho displayName nếu không null hoặc rỗng
            if (!string.IsNullOrEmpty(displayName))
            {
                // 'i' là ignore case
                filter &= filterBuilder.Regex(u => u.DisplayName, new BsonRegularExpression(displayName, "i"));
                cacheKey += $"_fullname_{displayName}";
            }

            // Kiểm tra và thêm điều kiện lọc cho gender nếu không null hoặc rỗng
            if (!string.IsNullOrEmpty(gender.ToString()))
            {
                filter &= filterBuilder.Eq(u => u.Gender, gender);
                cacheKey += $"_gender_{gender}";
            }

            // Kiểm tra và thêm điều kiện lọc cho email nếu không null hoặc rỗng
            if (!string.IsNullOrEmpty(email))
            {
                // 'i' là ignore case
                filter &= filterBuilder.Regex(u => u.Email, new BsonRegularExpression(email, "i"));
                cacheKey += $"_email_{email}";
            }

            // Lấy tất cả thông tin người dùng và áp dụng bộ lọc
            IEnumerable<UserResponseModel> users;

            users = await _unitOfWork.GetCollection<User>().Find(filter)
            .Project(users => new UserResponseModel
            {
                UserId = users.Id.ToString(),
                Role = users.Roles.ToString(),
                Email = users.Email,
                DisplayName = users.DisplayName,
                Gender = users.Gender.ToString(),
                Birthdate = users.Birthdate,
                //ImageResponseModel = users.ImageResponseModel,
                IsLinkedWithGoogle = users.IsLinkedWithGoogle,
                Status = users.Status.ToString(),
                CreatedTime = users.CreatedTime.ToString(),
                LastLoginTime = users.LastLoginTime.HasValue ? users.LastLoginTime.Value.ToString() : null,
                LastUpdatedTime = users.LastUpdatedTime.HasValue ? users.LastUpdatedTime.Value.ToString() : null,
            })
            .ToListAsync();

            return users;
        }

        public async Task<UserProfileResponseModel> GetProfileAsync()
        {
            // UserID lấy từ phiên người dùng có thể là FE hoặc BE
            string? userID = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Kiểm tra UserId
            if (string.IsNullOrEmpty(userID))
            {
                throw new UnauthorizedAccessException("Your session is limit, you must login again to edit profile!");
            }

            // Projection
            ProjectionDefinition<User> userProjection = Builders<User>.Projection
                .Include(user => user.Id)
                .Include(user => user.DisplayName)
                .Include(user => user.Images);

            // Lấy thông tin người dùng theo ID
            User user = await _unitOfWork.GetCollection<User>().Find(user => user.Id == userID)
                .Project<User>(userProjection)
                .FirstOrDefaultAsync();

            // Mapping từ User sang UserResponseModel
            UserProfileResponseModel userResponseModel = _mapper.Map<UserProfileResponseModel>(user);

            return userResponseModel;
        }

        public async Task EditProfileAsync(EditProfileRequestModel requestModel)
        {
            string? userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            User user = await _unitOfWork.GetCollection<User>()
                                         .Find(user => user.Id == userId)
                                         .Project(Builders<User>.Projection.Include(user => user.Images))
                                         .As<User>()
                                         .FirstOrDefaultAsync()
                        ?? throw new DataNotFoundCustomException("Not found any artist");

            // nếu không điền dữ liệu mới thì báo lỗi
            //string displayName = requestModel.DisplayName ?? throw new BadRequestCustomException("Display name is required!");

            //map từ model qua artist
            //_mapper.Map<EditProfileRequestModel, User>(requestModel, artist);

            // Build update definition
            UpdateDefinitionBuilder<User> updateBuilder = Builders<User>.Update;

            // Cập nhật các field sẵn có
            List<UpdateDefinition<User>> updates = new()
            {
                updateBuilder.Set(user => user.DisplayName, requestModel.DisplayName),
                updateBuilder.Set(user => user.LastUpdatedTime, Util.GetUtcPlus7Time())
            };

            // Cập nhật Image Field nếu có
            if (requestModel.Image is not null)
            {
                // Upload ảnh lên Cloudinary
                ImageUploadResult result = _cloudinaryService.UploadImage(requestModel.Image, ImageTag.Users_Profile);

                // Cập nhật URL cho ảnh
                updates.Add(updateBuilder.Set(user => user.Images, user.Images.Select(img => new Image { URL = result.SecureUrl.AbsoluteUri }).ToList()));
            }

            UpdateDefinition<User> updateDefinition = updateBuilder.Combine(updates);

            await _unitOfWork.GetCollection<User>().UpdateOneAsync(
                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                updateDefinition
            );
        }

        public async Task<AuthenticatedUserInfoResponseModel> SwitchToArtistProfile()
        {
            // Lấy UserId từ phiên người dùng
            string? userID = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Kiểm tra UserId
            if (string.IsNullOrEmpty(userID))
            {
                throw new UnauthorizedAccessException("Your session is limit, you must login again to edit profile!");
            }

            // Lấy thông tin artist
            Artist artist = await _unitOfWork.GetCollection<Artist>().Find(a => a.UserId == userID).FirstOrDefaultAsync() ?? throw new DataNotFoundCustomException("You are not an artist!");

            // Claim list
            IEnumerable<Claim> claims =
                [
                    new Claim(ClaimTypes.NameIdentifier, artist.UserId?.ToString() ?? throw new DataNotFoundCustomException("Not found any artist")),
                    new Claim("ArtistId", artist.Id.ToString()),
                    new Claim(ClaimTypes.Role, UserRole.Artist.ToString()),
                    new Claim(ClaimTypes.Name, artist.Name),
                    new Claim("Avatar", artist.Images[0].URL ?? throw new DataNotFoundCustomException("Not found any images"))
                ];

            // Gọi phương thức để tạo access token và refresh token từ danh sách claim và thông tin người dùng
            _jwtBLL.GenerateAccessToken(claims, userID, out string accessToken, out string refreshToken);

            // Tạo cookie
            CookieOptions cookieOptions = new()
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(7)
            };

            // Thêm refresh token vào cookie
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("SpotifyPool_RefreshToken");
            _httpContextAccessor.HttpContext.Response.Cookies.Append("SpotifyPool_RefreshToken", refreshToken, cookieOptions);

            // Tạo thông tin người dùng đã xác thực
            AuthenticatedUserInfoResponseModel authenticatedUserInfoResponseModel = new()
            {
                AccessToken = accessToken,
                Id = userID,
                ArtistId = artist.Id,
                Name = artist.Name,
                Role = [UserRole.Artist.ToString()],
                Avatar = [artist.Images[0].URL]
            };

            return authenticatedUserInfoResponseModel;
        }

        //test method
    }
}
