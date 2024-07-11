namespace Users.DAL.Models
{
    public class EntryRequest : BaseEntity
    {
        public long BaseUserId { get; set; }

        public long UniversityId { get; set; }

        public BaseUser BaseUser { get; set; }

        public University University { get; set; }
    }
}
