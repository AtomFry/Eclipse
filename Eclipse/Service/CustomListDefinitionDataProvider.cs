using Eclipse.Helpers;
using Eclipse.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    public class CustomListDefinitionDataProvider
    {
        public CustomListDefinitionDataProvider()
        {
        }

        public async Task<CustomListDefinition> GetCustomListDefinitionByIdAsync(string id)
        {
            return await CustomListDefinitionDataService.Instance.GetCustomListDefinitionByIdAsync(id);
        }

        public async Task SaveCustomListDefinitionAsync(CustomListDefinition customListDefinition)
        {
            await CustomListDefinitionDataService.Instance.SaveCustomListDefinitionAsync(customListDefinition);
        }

        public async Task DeleteCustomListDefinition(string id)
        {
            await CustomListDefinitionDataService.Instance.DeleteCustomListDefinitionAsync(id);
        }

        public async Task<IEnumerable<CustomListDefinition>> GetAllCustomListDefinitionsAsync()
        {
            return await Task.Run(() =>
            {
                return GetAllCustomListDefinitions();
            });
        }

        public IEnumerable<CustomListDefinition> GetAllCustomListDefinitions()
        {
            return CustomListDefinitionDataService.Instance.GetAllCustomListDefinitions();
        }

        public async Task SaveCustomListDefinitionsAsync(List<CustomListDefinition> customListDefinitions)
        {
            await CustomListDefinitionDataService.Instance.SaveCustomListDefinitionsAsync(customListDefinitions);
        }
    }
}
