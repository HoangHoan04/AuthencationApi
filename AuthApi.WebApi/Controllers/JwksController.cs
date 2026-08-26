using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
public class JwksController : ControllerBase
{
    private readonly IRsaKeyManager _rsaKeyManager;

    public JwksController(IRsaKeyManager rsaKeyManager)
    {
        _rsaKeyManager = rsaKeyManager;
    }

    [HttpGet(".well-known/jwks.json")]
    [HttpGet("api/jwks")]
    [ResponseCache(Duration = 3600)]
    public ActionResult<JwksResponse> GetJwks()
    {
        var jwks = _rsaKeyManager.GetJwks();
        return Ok(jwks);
    }
}
