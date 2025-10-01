using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class AccessTokenResponse
{
    public string tokenType;
    public string accessToken;
    public int expiresIn;
    public string refreshToken; 
}