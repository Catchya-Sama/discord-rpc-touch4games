using System.Globalization;
using System.Text;

namespace TouchRpc.Core;

public static class NicknamePolicy
{
    public const string GeneralTooltip = "TOUCH";
    public const int DiscordLargeTextMinLength = 2;
    public const int DiscordLargeTextMaxLength = 128;

    public static NicknameView Select(
        string? localRoleId,
        string? automaticNickname,
        string? automaticNicknameRoleId,
        string? manualNickname,
        string? manualNicknameRoleId)
    {
        string? nickname = null;
        var source = NicknameSource.GeneralFallback;
        var diagnostic = NicknameDiagnostic.AutomaticUnavailable;

        if (MatchesIdentity(localRoleId, automaticNicknameRoleId) && !string.IsNullOrWhiteSpace(automaticNickname))
        {
            nickname = automaticNickname.Trim();
            source = NicknameSource.AutomaticVerified;
            diagnostic = NicknameDiagnostic.None;
        }
        else if (!string.IsNullOrWhiteSpace(manualNickname))
        {
            if (manualNicknameRoleId is null)
            {
                nickname = manualNickname.Trim();
                source = NicknameSource.Manual;
                diagnostic = NicknameDiagnostic.ManualUnbound;
            }
            else if (MatchesIdentity(localRoleId, manualNicknameRoleId))
            {
                nickname = manualNickname.Trim();
                source = NicknameSource.Manual;
                diagnostic = NicknameDiagnostic.AutomaticUnavailable;
            }
            else
            {
                diagnostic = NicknameDiagnostic.ManualIdentityMismatch;
            }
        }

        if (nickname is null) return new NicknameView(null, source, GeneralTooltip, diagnostic);

        var textElements = CountTextElements(nickname);
        if (textElements < DiscordLargeTextMinLength)
            return new NicknameView(nickname, source, GeneralTooltip, NicknameDiagnostic.TooltipTooShort);
        if (textElements <= DiscordLargeTextMaxLength)
            return new NicknameView(nickname, source, nickname, diagnostic);

        return new NicknameView(
            nickname,
            source,
            TakeTextElements(nickname, DiscordLargeTextMaxLength),
            NicknameDiagnostic.TooltipTruncated);
    }

    private static bool MatchesIdentity(string? localRoleId, string? candidateRoleId) =>
        localRoleId is not null && candidateRoleId is not null &&
        string.Equals(localRoleId, candidateRoleId, StringComparison.Ordinal);

    private static int CountTextElements(string value)
    {
        var count = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(value);
        while (enumerator.MoveNext()) count++;
        return count;
    }

    private static string TakeTextElements(string value, int count)
    {
        var builder = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(value);
        while (count-- > 0 && enumerator.MoveNext()) builder.Append(enumerator.GetTextElement());
        return builder.ToString();
    }
}