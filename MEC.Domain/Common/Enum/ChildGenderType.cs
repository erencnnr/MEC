using System.ComponentModel.DataAnnotations;

namespace MEC.Domain.Common.Enum
{
    public enum ChildGenderType
    {
        [Display(Name = "Kız")]
        Female = 0,

        [Display(Name = "Erkek")]
        Male = 1
    }
}
