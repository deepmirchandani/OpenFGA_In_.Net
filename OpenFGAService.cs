using System.Text.Json;
using AutoMapper;
using Configurations;
using Helpers;
using ReqRespModels;
using Db.Models;
using Utilities.Logging;
using OpenFga.Sdk.Api;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Model;

namespace Services
{
    public class OpenFGAService : IOpenFGAService
    {
        public string AuthorizationModelId { get; private set; }
        private readonly IMapper _mapper;
        public OpenFGAService(IMapper mapper)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// get OpenFgaApi object wtihout StoreId, AuthorizationModelId
        /// </summary>
        /// <returns></returns>
        private OpenFgaApi GetFgaClientObj()
        {
            var fgaClient = new OpenFgaApi(new ClientConfiguration
            {
                ApiUrl = LocalConfig.OpenFGA.ApiUrl, //"http://localhost:8080", // Self-hosted OpenFGA URL
                DefaultHeaders = new Dictionary<string, string>
                                {
                                    { "Authorization", $"Bearer {LocalConfig.OpenFGA.PresharedKey}" }
                                }
            });
            return fgaClient;
        }

        /// <summary>
        /// get OpenFgaApi object by StoreId without AuthorizationModelId
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        private OpenFgaApi GetFgaClientObj(string storeId)
        {
            var fgaClient = new OpenFgaApi(new ClientConfiguration
            {
                ApiUrl = LocalConfig.OpenFGA.ApiUrl,
                StoreId = storeId,
                DefaultHeaders = new Dictionary<string, string>
                                {
                                    { "Authorization", $"Bearer {LocalConfig.OpenFGA.PresharedKey}" }
                                }
            });
            return fgaClient;
        }

        /// <summary>
        /// get OpenFgaApi object by StoreId, AuthorizationModelId
        /// </summary>
        /// <param name="storeId"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        private OpenFgaApi GetFgaClientObj(string storeId, string modelId)
        {
            var fgaClient = new OpenFgaApi(new ClientConfiguration
            {
                ApiUrl = LocalConfig.OpenFGA.ApiUrl,
                StoreId = storeId,
                AuthorizationModelId = modelId,
                DefaultHeaders = new Dictionary<string, string>
                                {
                                    { "Authorization", $"Bearer {LocalConfig.OpenFGA.PresharedKey}" }
                                }
            });
            return fgaClient;
        }

        /// <summary>
        /// Add new store in OpenFga
        /// </summary>
        /// <param name="name">store name</param>
        /// <returns></returns>
        public async Task<string> CreateStoreAsync(string name)
        {
            var fgaClient = GetFgaClientObj();
            var store = await fgaClient.CreateStore(new CreateStoreRequest(name));
            return store.Id;
        }

        public async Task<bool> DeleteStoreAsync(string storeId)
        {
            var fgaClient = GetFgaClientObj(storeId);
            await fgaClient.DeleteStore(storeId);
            return true;
        }

        /// <summary>
        /// add new AuthorizationModel in OpenFga Store
        /// </summary>
        /// <param name="storeId">openfga storeId</param>
        /// <returns>modelId</returns>
        public async Task<string> AddAuthorizationModelAsync(string storeId)
        {
            var fgaClient = GetFgaClientObj(storeId);
            var model = OpenFgaAuthorizationModelHelper.GetModel();
            var writeRequest = new WriteAuthorizationModelRequest
            {
                SchemaVersion = "1.1",
                TypeDefinitions = model
            };
            var response = await fgaClient.WriteAuthorizationModel(storeId, writeRequest);
            return response.AuthorizationModelId;
        }

        public async Task AddStoreRegionRelationAsync(string storeId, string modelId, List<TupleKey> tuples)
        {
            foreach (var tuple in tuples)
            {
                tuple.Object = tuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                || tuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                || tuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                    ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(tuple.Object) : tuple.Object;
                tuple.User = tuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                || tuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                || tuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                    ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(tuple.User) : tuple.User;
            }

            var fgaClient = GetFgaClientObj(storeId, modelId);
            var writeRequest = new WriteRequest
            {
                AuthorizationModelId = modelId,
                Writes = new WriteRequestWrites
                {
                    TupleKeys = tuples
                }
            };

            await fgaClient.Write(storeId, writeRequest);
        }

        /// <summary>
        /// Assign User To Site Access
        /// </summary>
        /// <param name="updateAccessRquestObj"></param>
        /// <returns>success status</returns>
        public async Task<AddDeleteResponseModel> AddDeleteRelation(string storeId, string modelId, ReadRequestTupleKey oldCheckExistTuple, TupleKeyWithoutCondition oldTuple, ReadRequestTupleKey newCheckExistTuple, TupleKey newTuple)
        {
            var fgaClient = GetFgaClientObj(storeId, modelId);
            try
            {
                var request = new WriteRequest();
                var DeletesTupleKeys = new List<TupleKeyWithoutCondition>();
                var AddTupleKeys = new List<TupleKey>();

                if (!string.IsNullOrEmpty(oldCheckExistTuple.User) &&
                    !string.IsNullOrEmpty(oldCheckExistTuple.Object) &&
                    !string.IsNullOrEmpty(oldCheckExistTuple.Relation))
                {
                    oldCheckExistTuple.Object = oldCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                                    || oldCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                                    || oldCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(oldCheckExistTuple.Object) : oldCheckExistTuple.Object;
                    oldCheckExistTuple.User = oldCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                                || oldCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                                || oldCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                                    ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(oldCheckExistTuple.User) : oldCheckExistTuple.User;

                    oldTuple.Object = oldTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                        || oldTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                        || oldTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(oldTuple.Object) : oldTuple.Object;
                    oldTuple.User = oldTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                        || oldTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                        || oldTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(oldTuple.User) : oldTuple.User;

                    bool oldTupleExists = await CheckTupleExistsAsync(storeId, modelId, oldCheckExistTuple);

                    if (oldTupleExists)
                    {
                        DeletesTupleKeys.Add(oldTuple);
                    }
                }
                if (!string.IsNullOrEmpty(newCheckExistTuple.User) &&
                    !string.IsNullOrEmpty(newCheckExistTuple.Object) &&
                    !string.IsNullOrEmpty(newCheckExistTuple.Relation))
                {
                    newCheckExistTuple.Object = newCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                                    || newCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                                    || newCheckExistTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(newCheckExistTuple.Object) : newCheckExistTuple.Object;
                    newCheckExistTuple.User = newCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                                    || newCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                                    || newCheckExistTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(newCheckExistTuple.User) : newCheckExistTuple.User;

                    newTuple.Object = newTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                        || newTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                        || newTuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(newTuple.Object) : newTuple.Object;
                    newTuple.User = newTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                        || newTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                        || newTuple.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(newTuple.User) : newTuple.User;

                    bool newTupleExists = await CheckTupleExistsAsync(storeId, modelId, newCheckExistTuple);

                    if (!newTupleExists)
                    {
                        AddTupleKeys.Add(newTuple);
                    }
                }

                if (DeletesTupleKeys.Count > 0)
                {
                    request.Deletes = new WriteRequestDeletes { TupleKeys = DeletesTupleKeys };
                }
                if (AddTupleKeys.Count > 0)
                {
                    request.Writes = new WriteRequestWrites { TupleKeys = AddTupleKeys };
                }

                if (request.Deletes != null || request.Writes != null)
                {
                    var response = await fgaClient.Write(storeId, request);
                    if (response != null && response.ToString() == "{}")
                    {
                        return (new AddDeleteResponseModel
                        {
                            isSuccess = true,
                            message = "Access updated successfully",
                            obj = response
                        });
                    }
                    else
                    {
                        return (new AddDeleteResponseModel
                        {
                            isSuccess = false,
                            message = "Failed! Assign User To Site Access",
                            obj = null
                        });
                    }
                }
                else
                {
                    return (new AddDeleteResponseModel
                    {
                        isSuccess = true,
                        message = "Access updated successfully",
                        obj = null
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AssignUserToSiteAccess API storeId : {storeId}, modelId : {modelId}", ex);
                return (new AddDeleteResponseModel
                {
                    isSuccess = false,
                    message = "Failed! Assign User To Site Access",
                    obj = null
                });
            }
        }

        /// <summary>
        /// Assign Site to applicaition for save in heirarchy data of site in openfga
        /// </summary>
        /// <param name="storeId">storeId from openfga</param>
        /// <param name="modelId">modelId from openfga</param>
        /// <param name="tuples">tuples to save in openfga</param>
        /// <returns>success status</returns>
        public async Task<bool> AddUpdateSiteData(string storeId, string modelId, Site siteData, bool isAssigned)
        {
            siteData.StoreOutletCountryName = OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(siteData.StoreOutletCountryName);
            siteData.StoreOutletStateAbbreviation = OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(siteData.StoreOutletStateAbbreviation);
            siteData.StoreOutletCity = OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(siteData.StoreOutletCity);

            var tuples = new List<TupleKey>();
            var countryStateTuple = new TupleKey($"{OpenFgaHierarchyHelper.AccessUnderConstant.Country}:{siteData.StoreOutletCountryName}",
                                                         OpenFgaHierarchyHelper.AccessTypeContant.Parent,
                                                         $"{OpenFgaHierarchyHelper.AccessUnderConstant.State}:{siteData.StoreOutletStateAbbreviation}");
            tuples.Add(countryStateTuple);
            var stateCityTuple = new TupleKey($"{OpenFgaHierarchyHelper.AccessUnderConstant.State}:{siteData.StoreOutletStateAbbreviation}",
                                                 OpenFgaHierarchyHelper.AccessTypeContant.Parent,
                                                 $"{OpenFgaHierarchyHelper.AccessUnderConstant.City}:{siteData.StoreOutletCity}");
            tuples.Add(stateCityTuple);
            var cityStoreoutletTuple = new TupleKey($"{OpenFgaHierarchyHelper.AccessUnderConstant.City}:{siteData.StoreOutletCity}",
                                                 OpenFgaHierarchyHelper.AccessTypeContant.Parent,
                                                 $"{OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet}:{siteData.TdlinxStoreOutletCode.ToString()}");
            tuples.Add(cityStoreoutletTuple);

            if (!string.IsNullOrEmpty(storeId) && !string.IsNullOrEmpty(modelId) && tuples != null && tuples.Count > 0)
            {
                var fgaClient = GetFgaClientObj(storeId, modelId);
                try
                {
                    var request = new WriteRequest();

                    var AddTupleKeys = new List<TupleKey>();
                    var DeletesTupleKeys = new List<TupleKeyWithoutCondition>();

                    foreach (var tuple in tuples)
                    {
                        var tupleCheckExist = _mapper.Map<TupleKey, ReadRequestTupleKey>(tuple);
                        if (!string.IsNullOrEmpty(tuple.User) &&
                            !string.IsNullOrEmpty(tuple.Object) &&
                            !string.IsNullOrEmpty(tuple.Relation))
                        {
                            bool newTupleExists = await CheckTupleExistsAsync(storeId, modelId, tupleCheckExist);

                            if (!newTupleExists)
                            {
                                if (isAssigned)
                                    AddTupleKeys.Add(tuple);
                            }
                            else if (tuple.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet) && isAssigned == false)
                            {
                                //Delete only storeOutlet in openFga
                                var deleteTupleWithoutCondition = new TupleKeyWithoutCondition(cityStoreoutletTuple.User,
                                                                                               cityStoreoutletTuple.Relation,
                                                                                               cityStoreoutletTuple.Object);
                                DeletesTupleKeys.Add(deleteTupleWithoutCondition);
                            }
                        }
                    }
                    if (AddTupleKeys.Count > 0)
                    {
                        request.Writes = new WriteRequestWrites { TupleKeys = AddTupleKeys };
                    }
                    if (DeletesTupleKeys.Count > 0)
                    {
                        request.Deletes = new WriteRequestDeletes { TupleKeys = DeletesTupleKeys };
                    }
                    if (request.Deletes != null || request.Writes != null)
                    {
                        var response = await fgaClient.Write(storeId, request);
                        if (response != null && response.ToString() == "{}")
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"AssignUnassignSiteToApplication API-storeId : {storeId}, modelId : {modelId}, tuples : {JsonSerializer.Serialize(tuples, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })}", ex);
                    return false;
                }
            }
            else { return false; }
        }

        private async Task<bool> CheckTupleExistsAsync(string storeId, string modelId, ReadRequestTupleKey tuple)
        {
            var fgaClient = GetFgaClientObj(storeId, modelId);
            var readRequest = new ReadRequest
            {
                TupleKey = tuple
            };

            var response = await fgaClient.Read(storeId, readRequest);

            // If any tuple returned, it exists
            return response.Tuples != null && response.Tuples.Count > 0;
        }

        /// <summary>
        /// Check Access in hierarchy
        /// </summary>
        /// <param name="storeId">storeId from openfga</param>
        /// <param name="modelId">modelId from openfga</param>
        /// <param name="userId">test.user@gmail.com</param>
        /// <param name="objectType">country / state / city / storeoutlet</param>
        /// <param name="objectId">USA / california / sanfrancisco / 100</param>
        /// <param name="relation">writer</param>
        /// <returns>true / false</returns>
        public async Task<bool> CheckAccessAsync(string storeId, string modelId, string userId, string objectType, string objectId, string relation)
        {
            objectId = objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country)
                                        || objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State)
                                        || objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City)
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(objectId) : objectId;

            var fgaClient = GetFgaClientObj(storeId, modelId);
            var checkResponse = await fgaClient.Check(storeId, new CheckRequest
            {
                TupleKey = new CheckRequestTupleKey
                {
                    User = $"{OpenFgaHierarchyHelper.AccessForContant.User}:{userId}",
                    Object = $"{objectType}:{objectId}",
                    Relation = $"{relation}",
                }
            });
            return checkResponse.Allowed == true;
        }

        /// <summary>
        /// Check Direct Access on only specific node without checking access on parent or child
        /// </summary>
        /// <param name="storeId">storeId from openfga</param>
        /// <param name="modelId">modelId from openfga</param>
        /// <param name="userId">test.user@gmail.com</param>
        /// <param name="objectType">country / state / city / storeoutlet</param>
        /// <param name="objectId">USA / california / sanfrancisco / 100</param>
        /// <param name="relation">writer</param>
        /// <returns></returns>
        public async Task<bool> CheckDirectAccessAsync(string storeId, string modelId, string userId, string objectType, string objectId, string relation)
        {
            objectId = objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country)
                                        || objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State)
                                        || objectType.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City)
                                            ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToSave(objectId) : objectId;

            var fgaClient = GetFgaClientObj(storeId, modelId);
            var directAccessResponse = await fgaClient.Read(storeId, new ReadRequest
            {
                TupleKey = new ReadRequestTupleKey
                {
                    User = $"{OpenFgaHierarchyHelper.AccessForContant.User}:{userId}",
                    Object = $"{objectType}:{objectId}",
                    Relation = $"{relation}",
                }
            });
            bool hasDirectAccess = directAccessResponse.Tuples.Any();
            return hasDirectAccess == true;
        }

        /// <summary>
        /// get site tree view
        /// </summary>
        /// <param name="storeId">storeId from openfga</param>
        /// <param name="modelId">modelId from openfga</param>
        /// <returns>site hierarchy</returns>
        public async Task<List<HierarchyNode>> GetHierarchy(string storeId, string modelId)
        {
            var fgaClient = GetFgaClientObj(storeId, modelId);
            var tuples = new List<OpenFga.Sdk.Model.Tuple>();

            string continuationToken = null;

            do
            {
                var response = await fgaClient.Read(
                    storeId: storeId,
                    body: new ReadRequest
                    {
                        ContinuationToken = continuationToken
                    }
                );

                if (response.Tuples != null)
                    tuples.AddRange(response.Tuples);

                continuationToken = response.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            foreach (var tuple in tuples)
            {
                tuple.Key.Object = tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                   || tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                   || tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToGet(tuple.Key.Object) : tuple.Key.Object;
                tuple.Key.User = tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                   || tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                   || tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToGet(tuple.Key.User) : tuple.Key.User;
            }

            // 1.Extract parent - child relationships
            var parentRelations = tuples
                .Where(t => t.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Parent)
                .Select(t => new OpenFgaParentRelation
                {
                    Parent = t.Key.User,
                    Child = t.Key.Object
                })
                .ToList();

            // 2. Extract access control (writers)
            var accessMap = new Dictionary<string, OpenFgaAccessInfo>();

            foreach (var tuple in tuples.Where(t => t.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Writer))
            {
                if (!accessMap.ContainsKey(tuple.Key.Object))
                {
                    accessMap[tuple.Key.Object] = new OpenFgaAccessInfo();
                }

                var user = tuple.Key.User;

                if (tuple.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Writer)
                    accessMap[tuple.Key.Object].Writers.Add(user);
            }

            // 3. Build the hierarchy
            var hierarchy = OpenFgaHierarchyHelper.BuildHierarchy(parentRelations, accessMap);

            return hierarchy;
        }

        ///Get Full Hierarchy by storeId & modelId regardless of whom access at what level
        public async Task<List<FullHierarchyNode>> GetFullHierarchy(string storeId, string modelId)
        {
            var fgaClient = GetFgaClientObj(storeId, modelId);
            var tuples = new List<OpenFga.Sdk.Model.Tuple>();

            string continuationToken = null;

            do
            {
                var response = await fgaClient.Read(
                    storeId: storeId,
                    body: new ReadRequest
                    {
                        ContinuationToken = continuationToken
                    }
                );

                if (response.Tuples != null)
                    tuples.AddRange(response.Tuples);

                continuationToken = response.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            foreach (var tuple in tuples)
            {
                tuple.Key.Object = tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                   || tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                   || tuple.Key.Object.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToGet(tuple.Key.Object) : tuple.Key.Object;
                tuple.Key.User = tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.Country + ":")
                                   || tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.State + ":")
                                   || tuple.Key.User.Contains(OpenFgaHierarchyHelper.AccessUnderConstant.City + ":")
                                        ? OpenFgaHierarchyHelper.OpenFGAFormatObjectToGet(tuple.Key.User) : tuple.Key.User;
            }

            // 1.Extract parent - child relationships
            var parentRelations = tuples
                .Where(t => t.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Parent)
                .Select(t => new OpenFgaParentRelation
                {
                    Parent = t.Key.User,
                    Child = t.Key.Object
                })
                .ToList();

            // 2. Extract access control (writers)
            var accessMap = new Dictionary<string, OpenFgaAccessInfo>();

            foreach (var tuple in tuples.Where(t => t.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Writer))
            {
                if (!accessMap.ContainsKey(tuple.Key.Object))
                {
                    accessMap[tuple.Key.Object] = new OpenFgaAccessInfo();
                }

                var user = tuple.Key.User;

                if (tuple.Key.Relation == OpenFgaHierarchyHelper.AccessTypeContant.Writer)
                    accessMap[tuple.Key.Object].Writers.Add(user);
            }

            // 3. Build the hierarchy
            var hierarchy = OpenFgaHierarchyHelper.BuildFullHierarchy(parentRelations, accessMap);

            return hierarchy;
        }
    }
}
