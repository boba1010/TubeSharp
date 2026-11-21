using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;

namespace TubeSharp.Json;

/// <inheritdoc/>
public sealed class UnescapedEmojiJsonEncoder : JavaScriptEncoder
{
    /// <inheritdoc/>
    public static UnescapedEmojiJsonEncoder Instance { get; } = new();

    /// <inheritdoc/>
    public override int MaxOutputCharactersPerInputCharacter => 12;

    /// <inheritdoc/>
    public override bool WillEncode(int unicodeScalar)
    {
        // Never encode supplementary plane characters (emojis >= U+10000)
        if (unicodeScalar >= 0x10000)
            return false;

        // Delegate BMP and ASCII escaping (quotes, backslashes, control chars)
        return UnsafeRelaxedJsonEscaping.WillEncode(unicodeScalar);
    }

    /// <inheritdoc/>
    public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
    {
        for (int i = 0; i < textLength; i++)
        {
            char c = text[i];

            // If we hit a valid surrogate pair, advance past both chars without encoding
            if (char.IsHighSurrogate(c) && i + 1 < textLength && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
                continue;
            }

            if (WillEncode(c))
                return i;
        }

        return -1;
    }

    /// <inheritdoc/>
    public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
    {
        if (unicodeScalar >= 0x10000)
        {
            var rune = new Rune(unicodeScalar);
            if (rune.Utf16SequenceLength <= bufferLength)
            {
                numberOfCharactersWritten = rune.EncodeToUtf16(new Span<char>(buffer, bufferLength));
                return true;
            }

            numberOfCharactersWritten = 0;
            return false;
        }

        return UnsafeRelaxedJsonEscaping.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
    }

    /// <inheritdoc/>
    public override OperationStatus EncodeUtf8(
        ReadOnlySpan<byte> utf8Source,
        Span<byte> utf8Destination,
        out int bytesConsumed,
        out int bytesWritten,
        bool isFinalBlock = true)
    {
        // Fast-path UTF-8 writer support used by Utf8JsonWriter
        int srcIdx = 0;
        int dstIdx = 0;

        while (srcIdx < utf8Source.Length)
        {
            OperationStatus status = Rune.DecodeFromUtf8(utf8Source[srcIdx..], out Rune rune, out int consumed);

            if (status == OperationStatus.NeedMoreData && !isFinalBlock)
            {
                bytesConsumed = srcIdx;
                bytesWritten = dstIdx;
                return OperationStatus.NeedMoreData;
            }

            if (status != OperationStatus.Done)
            {
                // Let UnsafeRelaxedJsonEscaping handle invalid sequences safely
                return UnsafeRelaxedJsonEscaping.EncodeUtf8(utf8Source, utf8Destination, out bytesConsumed, out bytesWritten, isFinalBlock);
            }

            if (!WillEncode(rune.Value))
            {
                if (dstIdx + consumed > utf8Destination.Length)
                {
                    bytesConsumed = srcIdx;
                    bytesWritten = dstIdx;
                    return OperationStatus.DestinationTooSmall;
                }

                utf8Source.Slice(srcIdx, consumed).CopyTo(utf8Destination.Slice(dstIdx));
                srcIdx += consumed;
                dstIdx += consumed;
            }
            else
            {
                // Delegate characters that must be escaped (", \, \n, etc.)
                var singleRuneSrc = utf8Source.Slice(srcIdx, consumed);
                var innerStatus = UnsafeRelaxedJsonEscaping.EncodeUtf8(
                    singleRuneSrc,
                    utf8Destination[dstIdx..],
                    out int innerConsumed,
                    out int innerWritten,
                    isFinalBlock: true);

                if (innerStatus != OperationStatus.Done)
                {
                    bytesConsumed = srcIdx;
                    bytesWritten = dstIdx;
                    return innerStatus;
                }

                srcIdx += innerConsumed;
                dstIdx += innerWritten;
            }
        }

        bytesConsumed = srcIdx;
        bytesWritten = dstIdx;
        return OperationStatus.Done;
    }
}