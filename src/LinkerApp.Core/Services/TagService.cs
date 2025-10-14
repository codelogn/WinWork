using LinkerApp.Models;
using LinkerApp.Data.Repositories;
using System.Text.RegularExpressions;

namespace LinkerApp.Core.Services;

/// <summary>
/// Service implementation for tag management operations
/// </summary>
public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _tagRepository.GetAllAsync();
    }

    public async Task<Tag?> GetTagAsync(int id)
    {
        return await _tagRepository.GetByIdAsync(id);
    }

    public async Task<Tag?> GetTagByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _tagRepository.GetByNameAsync(name.Trim());
    }

    public async Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Tag>();

        return await _tagRepository.SearchAsync(searchTerm.Trim());
    }

    public async Task<IEnumerable<Tag>> GetTagsForLinkAsync(int linkId)
    {
        return await _tagRepository.GetTagsForLinkAsync(linkId);
    }

    public async Task<Tag> CreateTagAsync(Tag tag)
    {
        if (!await ValidateTagAsync(tag))
            throw new ArgumentException("Invalid tag data", nameof(tag));

        // Check if tag with same name already exists
        var existingTag = await _tagRepository.GetByNameAsync(tag.Name);
        if (existingTag != null)
            throw new InvalidOperationException($"Tag with name '{tag.Name}' already exists");

        // Ensure color is in correct format
        tag.Color = NormalizeColor(tag.Color);

        return await _tagRepository.CreateAsync(tag);
    }

    public async Task<Tag> UpdateTagAsync(Tag tag)
    {
        if (!await ValidateTagAsync(tag))
            throw new ArgumentException("Invalid tag data", nameof(tag));

        var existingTag = await _tagRepository.GetByIdAsync(tag.Id);
        if (existingTag == null)
            throw new InvalidOperationException($"Tag with ID {tag.Id} not found");

        // Check if another tag with the same name exists
        var duplicateTag = await _tagRepository.GetByNameAsync(tag.Name);
        if (duplicateTag != null && duplicateTag.Id != tag.Id)
            throw new InvalidOperationException($"Another tag with name '{tag.Name}' already exists");

        // Ensure color is in correct format
        tag.Color = NormalizeColor(tag.Color);

        return await _tagRepository.UpdateAsync(tag);
    }

    public async Task<bool> DeleteTagAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
            return false;

        // Check if tag is in use
        if (tag.LinkTags.Any())
        {
            throw new InvalidOperationException("Cannot delete tag that is assigned to links. Remove tag from links first.");
        }

        return await _tagRepository.DeleteAsync(id);
    }

    public async Task<bool> AddTagToLinkAsync(int linkId, int tagId)
    {
        // Validate that both link and tag exist
        // (In a real implementation, you might want to check with LinkRepository too)
        var tag = await _tagRepository.GetByIdAsync(tagId);
        if (tag == null)
            return false;

        return await _tagRepository.AddTagToLinkAsync(linkId, tagId);
    }

    public async Task<bool> RemoveTagFromLinkAsync(int linkId, int tagId)
    {
        return await _tagRepository.RemoveTagFromLinkAsync(linkId, tagId);
    }

    public async Task<bool> ValidateTagAsync(Tag tag)
    {
        if (tag == null)
            return false;

        if (string.IsNullOrWhiteSpace(tag.Name))
            return false;

        // Validate name length and characters
        if (tag.Name.Length > 100)
            return false;

        // Validate color format
        if (!IsValidColor(tag.Color))
            return false;

        return await Task.FromResult(true);
    }

    private bool IsValidColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        // Check for hex color format (#RRGGBB or #RGB)
        var hexPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
        return Regex.IsMatch(color, hexPattern);
    }

    private string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return "#808080"; // Default gray

        color = color.Trim();
        
        // Add # if missing
        if (!color.StartsWith("#"))
            color = "#" + color;

        // Convert 3-digit hex to 6-digit
        if (color.Length == 4 && Regex.IsMatch(color, @"^#[A-Fa-f0-9]{3}$"))
        {
            color = $"#{color[1]}{color[1]}{color[2]}{color[2]}{color[3]}{color[3]}";
        }

        // Validate and return
        return IsValidColor(color) ? color.ToUpper() : "#808080";
    }
}