namespace MealsService.Users.Data
{
    public class UserDto
    {
        public int UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Avatar { get; set; }

        public string Bio { get; set; }

        public string Birthdate { get; set; }

        public string Gender { get; set; }

        public string Email { get; set; }

        public string Location { get; set; }

        public float GeoLat { get; set; }

        public float GeoLong { get; set; }

        public string Timezone { get; set; }
    }
}
