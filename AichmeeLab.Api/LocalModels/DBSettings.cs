using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AichmeeLab.Api.LocalModels
{
    class DBSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;

        public string ArticlesCollectionName { get; set; } = string.Empty;
    }
}
