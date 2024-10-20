namespace TheBestLogger.Core.Utilities
{
    public interface ITagsRegistry
    {
        // Adds a tag to the collection
        bool AddTag(string tag);

        // Removes a tag from the collection
        bool RemoveTag(string tag);

        // Checks if a tag exists in the collection
        bool ContainsTag(string tag);

        // Retrieves all tags in the collection
        string[] GetAllTags();
    }

}
