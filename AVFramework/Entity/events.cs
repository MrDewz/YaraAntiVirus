//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AVFramework.Entity
{
    using System;
    using System.Collections.Generic;
    
    public partial class events
    {
        public int id { get; set; }
        public Nullable<int> computer_id { get; set; }
        public Nullable<System.DateTime> event_date { get; set; }
        public Nullable<int> virus_id { get; set; }
        public string ip_address { get; set; }
        public Nullable<int> action_id { get; set; }
    
        public virtual actions actions { get; set; }
        public virtual computers computers { get; set; }
        public virtual viruses viruses { get; set; }
    }
}
