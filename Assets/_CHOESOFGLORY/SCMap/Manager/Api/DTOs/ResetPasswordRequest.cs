using System;

[Serializable]
public class ResetPasswordRequest
{
    public string email;
    public string newPassword;
    public string resetCode;
}