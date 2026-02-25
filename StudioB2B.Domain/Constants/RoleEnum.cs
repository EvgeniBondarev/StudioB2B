using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum RoleEnum
{
    [Description("Директор")]
    Administrator = 1,

    [Description("Сотрудник")]
    User = 2
}
