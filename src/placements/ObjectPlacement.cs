
namespace BeatThat
{
	public interface ObjectPlacement<T> where T : class
	{
		T managedObject { get; }
		
		void EnsureCreated();
		
		void Delete();
		
	}
}