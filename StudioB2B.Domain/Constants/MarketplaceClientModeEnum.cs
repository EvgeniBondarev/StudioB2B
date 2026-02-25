using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum MarketplaceClientModeEnum
{
    [Description("FBS")]
    Fbs = 1,

    [Description("FBO")]
    Fbo = 2,

    [Description("Express")]
    Express = 3
}
