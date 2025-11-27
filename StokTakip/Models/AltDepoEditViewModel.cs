using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace StokTakip.Models
{
    public class AltDepoEditViewModel
    {
        public AltDepo AltDepo { get; set; } = null!;
        public int SeciliAnaDepoId { get; set; }
        public IEnumerable<SelectListItem> AnaDepoListesi { get; set; } = null!;
    }
}