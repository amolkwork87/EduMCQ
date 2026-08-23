namespace EduMcq.Common;

public static class Helper
{

    public string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    public string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+91")) return phone;
        if (phone.StartsWith("91") && phone.Length == 12) return "+" + phone;
        if (phone.Length == 10) return "+91" + phone;
        return phone; // already in E.164 hopefully
    }

   
}
