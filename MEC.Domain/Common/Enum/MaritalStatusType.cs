using System.ComponentModel.DataAnnotations;

namespace MEC.Domain.Common.Enum
{
    public enum MaritalStatusType
    {
        [Display(Name = "Bekar")]
        Single = 0,

        [Display(Name = "Evli")]
        Married = 1
    }
}
