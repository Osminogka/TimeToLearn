namespace Users.DAL.Models
{
    public class Student : BaseEntity
    {
        public long BaseUserId { get; set; }

        public BaseUser BaseUser { get; set; }
    }
}
