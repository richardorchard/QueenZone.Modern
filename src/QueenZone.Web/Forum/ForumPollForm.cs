using System.ComponentModel.DataAnnotations;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>Bound fields for the optional poll section on new-thread.</summary>
public sealed class ForumPollForm
{
    public bool Enabled { get; set; }

    [StringLength(EfForumPollRepository.QuestionMaxLength)]
    public string? Question { get; set; }

    public bool IsMultiChoice { get; set; }

    [Range(1, EfForumPollRepository.MaxOptions)]
    public int? MaxChoices { get; set; }

    public DateTimeOffset? ClosesAt { get; set; }

    public List<string> Options { get; set; } = ["", ""];

    public NewForumPoll? ToNewPoll(Guid memberId, List<string> errors)
    {
        if (!Enabled)
        {
            return null;
        }

        var question = Question?.Trim() ?? string.Empty;
        if (question.Length == 0)
        {
            errors.Add("Poll question is required.");
        }
        else if (question.Length > EfForumPollRepository.QuestionMaxLength)
        {
            errors.Add($"Poll question must be at most {EfForumPollRepository.QuestionMaxLength} characters.");
        }

        var options = (Options ?? [])
            .Select(option => option?.Trim() ?? string.Empty)
            .Where(option => option.Length > 0)
            .ToList();

        if (options.Count < EfForumPollRepository.MinOptions)
        {
            errors.Add($"Add at least {EfForumPollRepository.MinOptions} poll options.");
        }

        if (options.Count > EfForumPollRepository.MaxOptions)
        {
            errors.Add($"Polls support at most {EfForumPollRepository.MaxOptions} options.");
        }

        if (options.Any(option => option.Length > EfForumPollRepository.OptionMaxLength))
        {
            errors.Add($"Each poll option must be at most {EfForumPollRepository.OptionMaxLength} characters.");
        }

        if (IsMultiChoice && MaxChoices is int max && max < 1)
        {
            errors.Add("Max choices must be at least 1.");
        }

        if (ClosesAt is DateTimeOffset closesAt && closesAt <= DateTimeOffset.UtcNow)
        {
            errors.Add("Poll close time must be in the future.");
        }

        if (errors.Count > 0)
        {
            return null;
        }

        return new NewForumPoll(
            question,
            IsMultiChoice,
            IsMultiChoice ? MaxChoices : null,
            ClosesAt,
            options,
            memberId);
    }
}
