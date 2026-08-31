using Softwareproject.Models;

namespace Softwareproject.Data
{
    public interface IStatisticRepository
    {
        List<Statistic> GetAll();

        Statistic? GetLatestForTrainee(int traineeId);

        List<Statistic> GetHistoryForTrainee(int traineeId);

        void Create(Statistic statistic);

        void Update(Statistic statistic);

        void Delete(int id);
    }
}