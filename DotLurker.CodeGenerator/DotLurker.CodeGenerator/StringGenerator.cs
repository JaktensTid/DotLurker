using System.Text;

namespace DotLurker.CodeGenerator;

public class StringGenerator
{
    public string GenerateRandomName()
    {
        var firstBigLetter = new Random().RandomBoolean();
        var firstUnderscore = new Random().RandomBoolean();

        var sb = new StringBuilder();
        if (firstUnderscore)
            sb.Append('_');

        if (firstBigLetter)
            sb.Append(GenerateRandomString(1, true, false));

        sb.Append(GenerateRandomString(6, false, false));
        return sb.ToString();
    }

    public string GenerateRandomString(int maxLength, bool firstBigLetter = false, bool firstUnderscore = false, string additionalSymbols = "")
    {
        var allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXIYZabcdefghijkmnopqrstuvwxyzi" + additionalSymbols;
        var allowedCharsAndNumbers = allowedChars + "0123456789_";
        var chars = new char[maxLength];

        for (var i = 0; i < maxLength; i++)
        {
            if (firstUnderscore && i == 0)
            {
                chars[i] = '_';
                continue;
            }

            if (i == 0)
                chars[i] = allowedChars[new Random().Next(0, allowedChars.Length)];
            else
                chars[i] = allowedCharsAndNumbers[new Random().Next(0, allowedCharsAndNumbers.Length)];

            if (firstBigLetter && i == 0)
            {
                chars[i] = char.ToUpper(chars[i]);
            }
        }

        return new string(chars);
    }
}