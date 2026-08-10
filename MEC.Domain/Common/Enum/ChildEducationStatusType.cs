using System.ComponentModel.DataAnnotations;

namespace MEC.Domain.Common.Enum
{
    public enum ChildEducationStatusType
    {
        [Display(Name = "Eğitim Almıyor")]
        NotInEducation = 0,

        [Display(Name = "Okul Öncesi")]
        Preschool = 1,

        [Display(Name = "İlkokul")]
        PrimarySchool = 2,

        [Display(Name = "Ortaokul")]
        MiddleSchool = 3,

        [Display(Name = "Lise")]
        HighSchool = 4,

        [Display(Name = "Ön Lisans")]
        AssociateDegree = 5,

        [Display(Name = "Lisans")]
        Undergraduate = 6,

        [Display(Name = "Yüksek Lisans")]
        Graduate = 7,

        [Display(Name = "Doktora")]
        Doctorate = 8,

        [Display(Name = "Mezun")]
        Graduated = 9
    }
}
