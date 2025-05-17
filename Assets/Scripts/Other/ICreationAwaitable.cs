using System.Threading.Tasks;

public interface ICreationAwaitable
{
    Task WaitForCreationAsync();
}