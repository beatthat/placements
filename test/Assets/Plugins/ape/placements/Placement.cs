
namespace BeatThat
{
	/// <summary>
	/// a Placement provides access to an object, with methods to ensure it's creation and destruction.
	/// </summary>
	public interface Placement<T> where T : class
	{
		T managedObject { get; }
		
		void EnsureCreated();
		
		void Delete();
		
	}
}