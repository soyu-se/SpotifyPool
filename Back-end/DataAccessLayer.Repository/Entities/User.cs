﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SetupLayer.Enum.Services.User;

namespace DataAccessLayer.Repository.Entities
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Email { get; set; } = null!;

        public List<UserRole> Roles {  get; set; } = [];
        public UserProduct? Product { get; set; }
        public string CountryId { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public required string DisplayName { get; set; }
        public int Followers { get; set; }
        public List<Image> Images { get; set; } = [];
        public string? Birthdate { get; set; }
        public UserGender Gender { get; set; }
        public UserStatus Status { get; set; }
        public string? TokenEmailConfirm { get; set; }
        public bool? IsLinkedWithGoogle { get; set; } = null;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public DateTime CreatedTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime? LastUpdatedTime { get; set; }
        public Product? LatestPremium {  get; set; }
    }
    public class Product
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PremiumId { get; set; }
        public DateTime? BuyedTime { get; set; }
        public DateTime? ExpiredTime { get; set; }

    }
}
