using OpenFga.Sdk.Model;
using static Helpers.OpenFgaHierarchyHelper;
using TypeDefinition = OpenFga.Sdk.Model.TypeDefinition;

namespace AccessAPIBase.Helpers
{
    /// <summary>
    /// OpenFga Authorization Model Helper
    /// </summary>
    public static class OpenFgaAuthorizationModelHelper
    {
        /// <summary>
        /// DSL formatted Model
        //model
        //    schema 1.1

        //type country
        //  relations
        //    define writer: [user]

        //        type state
        //  relations
        //    define writer: [user] or writer from country

        //type city
        //  relations
        //    define writer: [user] or writer from state

        //type storeoutlet
        //  relations
        //    define writer: [user] or writer from city


        //Explanation
        //Object      | Parent Types          | Writer Inheritance
        //-----------------------------------------------------------
        //storeoutlet | city, state, country  | parent->writer
        //city        | state, country        | parent->writer
        //state       | country               | parent->writer
        //country     | none                  | directly user

        //sample
        //city:city123#writer@user:user456
        //storeoutlet:174#parent@city:city123
        //device:device789#parent@storeoutlet:174

        /// <summary>
        /// Get AuthorizationModel for OpenFga
        /// </summary>
        /// <returns></returns>
        public static List<TypeDefinition> GetModel()
        {
            return new List<TypeDefinition>
            {
                new TypeDefinition
                {
                    Type = OpenFgaHierarchyHelper.AccessForContant.User
                },

                new TypeDefinition
                {
                    Type = OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet,
                    Relations = new Dictionary<string, Userset>
                    {
                        { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new Userset { This = new object() } },
                        { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new Userset {
                            Union = new Usersets {
                                Child = new List<Userset> {
                                    new Userset { This = new object() },
                                    new Userset {
                                        TupleToUserset = new TupleToUserset
                                        {
                                            Tupleset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Parent },
                                            ComputedUserset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Writer }
                                        }
                                    }
                                }
                            }
                        }}
                    },
                    Metadata = new Metadata
                    {
                        Relations = new Dictionary<string, RelationMetadata>
                        {
                            { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.State },
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.City },
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.Country }
                                    }
                                }
                            },
                            { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessForContant.User }
                                    }
                                }
                            }
                        }
                    }
                },

                new TypeDefinition
                {
                    Type = OpenFgaHierarchyHelper.AccessUnderConstant.City,
                    Relations = new Dictionary<string, Userset>
                    {
                        { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new Userset { This = new object() } },
                        { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new Userset {
                            Union = new Usersets {
                                Child = new List<Userset> {
                                    new Userset { This = new object() },
                                    new Userset {
                                        TupleToUserset = new TupleToUserset
                                        {
                                            Tupleset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Parent },
                                            ComputedUserset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Writer }
                                        }
                                    }
                                }
                            }
                        }}
                    },
                    Metadata = new Metadata
                    {
                        Relations = new Dictionary<string, RelationMetadata>
                        {
                            { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.State },
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.Country }
                                    }
                                }
                            },
                            { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessForContant.User }
                                    }
                                }
                            }
                        }
                    }
                },

                new TypeDefinition
                {
                    Type = OpenFgaHierarchyHelper.AccessUnderConstant.State,
                    Relations = new Dictionary<string, Userset>
                    {
                        { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new Userset { This = new object() } },
                        { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new Userset {
                            Union = new Usersets {
                                Child = new List<Userset> {
                                    new Userset { This = new object() },
                                    new Userset {
                                        TupleToUserset = new TupleToUserset
                                        {
                                            Tupleset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Parent },
                                            ComputedUserset = new ObjectRelation { Relation = OpenFgaHierarchyHelper.AccessTypeContant.Writer }
                                        }
                                    }
                                }
                            }
                        }}
                    },
                    Metadata = new Metadata
                    {
                        Relations = new Dictionary<string, RelationMetadata>
                        {
                            { OpenFgaHierarchyHelper.AccessTypeContant.Parent, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessUnderConstant.Country }
                                    }
                                }
                            },
                            { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessForContant.User }
                                    }
                                }
                            }
                        }
                    }
                },

                new TypeDefinition
                {
                    Type = OpenFgaHierarchyHelper.AccessUnderConstant.Country,
                    Relations = new Dictionary<string, Userset>
                    {
                        { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new Userset { This = new object() } }
                    },
                    Metadata = new Metadata
                    {
                        Relations = new Dictionary<string, RelationMetadata>
                        {
                            { OpenFgaHierarchyHelper.AccessTypeContant.Writer, new RelationMetadata
                                {
                                    DirectlyRelatedUserTypes = new List<RelationReference>
                                    {
                                        new RelationReference { Type = OpenFgaHierarchyHelper.AccessForContant.User }
                                    }
                                }
                            }
                        }
                    }
                },
            };
        }

    }
}
