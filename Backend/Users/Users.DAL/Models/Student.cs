namespace Users.DAL.Models
{
    public class Student : BaseEntity
    {
        public long? UniversityId { get; set; }

        public University University { get; set; }

        public long BaseUserId { get; set; }

        public BaseUser BaseUser { get; set; }
    }
}
