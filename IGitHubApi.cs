using System.Threading.Tasks;

namespace UnitTests
{
    using Refit;

    public interface IGitHubApi
    {
        [Get("/foo")]
        Task<string> GetFooAsync();
    }
}
