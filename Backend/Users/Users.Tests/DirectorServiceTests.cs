using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;
using Users.DL.Services;

namespace Users.Tests
{
    public class DirectorServiceTests
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private DirectorService Service { get; set; }

        private readonly string UniversityName = "DKU";
        private readonly string DirectorEmail = "director@test.com";
        private readonly string Username = "Osminogka";
        private readonly string StudentName = "Student";
        private readonly string TeacherName = "Teacher";

        public DirectorServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbDirector"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new DirectorService(UniversityRepository, EntryRequestRepository, UserRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                IsTeacher = false,
            };
            context.Add(user);
            
            var student = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Student",
                Email = "student@test.com",
                IsTeacher = false,
                StudentId = 1
            };
            context.Add(student);
            
            var teacher = new BaseUser()
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Teacher",
                Email = "teacher@test.com",
                IsTeacher = true
            };

            var teacherStruct = new Teacher()
            {
                BaseUserId = teacher.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(teacher);
            context.Add(teacherStruct);
            
            var director = new BaseUser()
            {
                Id = 4,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "director@test.com",
                IsTeacher = true,
                UniversityId = 1
            };
            context.Add(director);

            var university = new University()
            {
                Id = 1,
                Name = "DKU",
                Address = new Address
                {
                    City = "Almaty",
                    Country = "Kaz",
                    Street = "Pushkina"
                },
                Description = "Test",
                IsOpened = true,
                DirectorId = director.Id
            };
            context.Add(university);

            var entryRequest = new EntryRequest
            {
                BaseUserId = 1,
                UniversityId = 1,
                SentByUniversity = false
            };
            context.Add(entryRequest);

            context.SaveChanges();
        }

        [Fact]
        public async Task AcceptEntryRequestTest()
        {
            //Arrange
            var model = new EntryRequestModel()
            {
                Username = Username,
                University = UniversityName
            };
            
            //Act
            var result = await Service.AcceptEntryRequestAsync(model, DirectorEmail);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Username == Username);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            Assert.True(response.Success);
            Assert.NotNull(user!.UniversityId);
        }

        [Fact]
        public async Task RejectEntryRequestTest()
        {
            //Arrange
            var model = new EntryRequestModel()
            {
                Username = Username,
                University = UniversityName
            };
            
            //Act
            var result = await Service.RejectEntryRequestAsync(model, DirectorEmail);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Username == Username);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            Assert.True(response.Success);
            Assert.Null(user!.UniversityId);
        }

        [Fact]
        public async Task UpdateUniversityInfoTest()
        {
            //Arrange
            UpdateUniversityInfoModel model = new UpdateUniversityInfoModel()
            {
                Name = "DKU",
                Description = "New description",
                Address = new Address
                {
                    Country = "USA",
                    City = "New York",
                    Street = "Daun street"
                }
            };

            //Act
            var result = await Service.UpdateUniversityInfoAsync(model, DirectorEmail);

            var university = await UniversityRepository.SingleOrDefaultAsync(obj => obj.Name == UniversityName);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            Assert.True(response.Success);
            Assert.Equal("USA", university!.Address.Country);

            //Arrange
            UpdateUniversityInfoModel model2 = new UpdateUniversityInfoModel()
            {
                Name = "DKU",
                Description = "New description",
                Address = new Address
                {
                    Country = "USA",
                    City = null,
                    Street = null
                }
            };

            //Act
            result = await Service.UpdateUniversityInfoAsync(model2, DirectorEmail);

            university = await UniversityRepository.SingleOrDefaultAsync(obj => obj.Name == UniversityName);

            //Assert
            var response2 = Assert.IsType<ResponseMessage>(result);

            Assert.True(response2.Success);
            Assert.Null(university!.Address.City);
        }

        [Fact]
        public async Task InviteStudentToUniversityTest()
        {
            //Act
            var result = await Service.InviteStudentToUniversityAsync(UniversityName, StudentName, DirectorEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj =>
                obj.BaseUser.Username == StudentName && obj.SentByUniversity == true);
            
            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            
            Assert.NotNull(invite);
            Assert.True(response.Success);
        }
        
        [Fact]
        public async Task InviteTeacherToUniversityTest()
        {
            //Act
            var result = await Service.InviteTeacherToUniversityAsync(UniversityName, TeacherName, DirectorEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj =>
                obj.BaseUser.Username == TeacherName && obj.SentByUniversity == true);
            
            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            
            Assert.NotNull(invite);
            Assert.True(response.Success);
        }
    }
}
