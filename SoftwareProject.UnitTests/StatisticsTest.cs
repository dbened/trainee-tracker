using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Softwareproject.Data;
using Softwareproject.Models;
using Softwareproject.Services;
using SoftwareProject.UnitTests.FakeHttp;
using Moq;
using Xunit;

namespace SoftwareProject.UnitTests
{
    
    public class StatisticsRefreshServiceTests
    {
        private readonly Mock<IStatisticRepository> _statisticRepositoryMock;
        private readonly Mock<ITraineeRepository> _traineeRepositoryMock;
        private readonly Mock<IMentorRepository> _mentorRepositoryMock;
        private readonly Mock<ITraineeLessonProgressRepository> _progressRepositoryMock;

        public StatisticsRefreshServiceTests()
        {
            _statisticRepositoryMock = new Mock<IStatisticRepository>();
            _traineeRepositoryMock = new Mock<ITraineeRepository>();
            _mentorRepositoryMock = new Mock<IMentorRepository>();
            _progressRepositoryMock = new Mock<ITraineeLessonProgressRepository>();
        }
        
        [Fact]
        public async Task RefreshAsync_WhenTraineeDoesNotExist_ShouldReturnEarly()
        {
            // Arrange
            var httpClient = new HttpClient(new FakeHttpMessageHandler());

            var service = new StatisticsRefreshService(
                _statisticRepositoryMock.Object,
                _traineeRepositoryMock.Object,
                _mentorRepositoryMock.Object,
                _progressRepositoryMock.Object,
                httpClient);

            _traineeRepositoryMock.Setup(r => r.GetByIdWithLessonPlan(It.IsAny<int>()))
                                  .Returns((Trainee)null);

            // Act
            await service.RefreshAsync(1);

            // Assert
            _statisticRepositoryMock.VerifyNoOtherCalls();
        }
        
        // Test nur bei API-Verfügbarkeit möglich, daher auskommentiert.
        /* [Fact]
        public async Task RefreshAsync_WhenApiFails_ShouldKeepPreviousStatisticsk()
        {
            // Arrange
            Statistic? updatedStatistic = null;

            // Capture the statistic passed to Update().
            _statisticRepositoryMock
                .Setup(r => r.Update(It.IsAny<Statistic>()))
                .Callback<Statistic>(s => updatedStatistic = s);

            var lessonPlan = new LessonPlan
            {
                Id = 1
            };

            var trainee = new Trainee
            {
                Id = 1,
                EmailAddress = "test@example.com",
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6)),
                LessonPlan = lessonPlan
            };

            // Existing statistic that should remain unchanged
            var existingStatistic = new Statistic
            {
                Trainee = trainee,
                Date = DateTime.Today.AddDays(-1),
                CompletedDays = 5,
                RemainingDays = 2,
                PresentDays = 6,
                BufferDays = -1,
                Speed = 83.33,
                ForecastBufferDays = -0.5,
                WorkingHoursAvailable = true,
                LastUpdated = DateTime.Today.AddDays(-1)
            };

            // Store the previous timestamp so it can be compared later.
            var previousUpdate = existingStatistic.LastUpdated;

            _traineeRepositoryMock
                .Setup(r => r.GetByIdWithLessonPlan(1))
                .Returns(trainee);

            _statisticRepositoryMock
                .Setup(r => r.GetLatestForTrainee(1))
                .Returns(existingStatistic);

            // Fake404Handler simulates a failing working-hours API.
            var service = new StatisticsRefreshService(
                _statisticRepositoryMock.Object,
                _traineeRepositoryMock.Object,
                _mentorRepositoryMock.Object,
                _progressRepositoryMock.Object,
                new HttpClient(new Fake404Handler()));

            // Act
            await service.RefreshAsync(1);

            // Assert
            Assert.NotNull(updatedStatistic);

            // Existing statistics should remain unchanged.
            Assert.Equal(5, updatedStatistic!.CompletedDays);
            Assert.Equal(2, updatedStatistic.RemainingDays);
            Assert.Equal(6, updatedStatistic.PresentDays);
            Assert.Equal(-1, updatedStatistic.BufferDays);
            Assert.Equal(83.33, updatedStatistic.Speed);
            Assert.Equal(-0.5, updatedStatistic.ForecastBufferDays);

            // Only the API availability flag should change.
            Assert.False(updatedStatistic.WorkingHoursAvailable);

            // LastUpdated should be refreshed.
            Assert.True(updatedStatistic.LastUpdated > previousUpdate);
        } */
        
        // Test nur bei API-Verfügbarkeit möglich, daher auskommentiert.
        /* [Fact]
        public async Task RefreshAsync_WithAPIResponse_ShouldCalculateCorrectly()
        {
            Statistic? updatedStatistic = null;

            _statisticRepositoryMock
                .Setup(r => r.Update(It.IsAny<Statistic>()))
                .Callback<Statistic>(s => updatedStatistic = s);

            var lessonPlan = new LessonPlan
            {
                Id = 1
            };

            var trainee = new Trainee
            {
                Id = 1,
                EmailAddress = "test@example.com",
                StartDate = new DateOnly(2024, 1, 1),
                LessonPlan = lessonPlan,
            };

            var progress1 = new TraineeLessonProgress
            {
                Trainee = trainee,
                Lesson = new Lesson
                {
                    Title = "Lesson 1",
                    URL = "https://example.com/1",
                    EstimatedDays = 5
                },
                Status = Status.OPEN
            };

            var progress2 = new TraineeLessonProgress
            {
                Trainee = trainee,
                Lesson = new Lesson
                {
                    Title = "Lesson 2",
                    URL = "https://example.com/2",
                    EstimatedDays = 3
                },
                Status = Status.FINISHED
            };

            var progress3 = new TraineeLessonProgress
            {
                Trainee = trainee,
                Lesson = new Lesson
                {
                    Title = "Lesson 3",
                    URL = "https://example.com/3",
                    EstimatedDays = 2
                },
                Status = Status.REJECTED
            };

            _traineeRepositoryMock
                .Setup(r => r.GetByIdWithLessonPlan(1))
                .Returns(trainee);

            _progressRepositoryMock
                .Setup(r => r.GetAll())
                .Returns(new List<TraineeLessonProgress>
                {
            progress1,
            progress2,
            progress3
                });

            _statisticRepositoryMock
                .Setup(r => r.GetLatestForTrainee(1))
                .Returns(new Statistic());

            var service = new StatisticsRefreshService(
                _statisticRepositoryMock.Object,
                _traineeRepositoryMock.Object,
                _mentorRepositoryMock.Object,
                _progressRepositoryMock.Object,
                new HttpClient(new FakeHttpMessageHandler()));

            // Act
            await service.RefreshAsync(1);

            // Assert
            Assert.NotNull(updatedStatistic);

            Assert.Equal(3.7, updatedStatistic!.CompletedDays, 1);

            Assert.Equal(6.3, updatedStatistic.RemainingDays, 1);

            Assert.Equal(3.0, updatedStatistic.PresentDays);

            Assert.Equal(0.7, updatedStatistic.BufferDays, 1);

            Assert.Equal(123.3333, updatedStatistic.Speed, 3);

            Assert.Equal(1.1919, updatedStatistic.ForecastBufferDays, 3);

            Assert.True(updatedStatistic.WorkingHoursAvailable);

            Assert.Equal(DateTime.Today, updatedStatistic.Date);
        } */

    }
}
