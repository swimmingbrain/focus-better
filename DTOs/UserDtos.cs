namespace MonkMode.DTOs
{
    public class UserProfileDto
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }
    }

    public class UpdateProfileDto
    {
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }
    }
}