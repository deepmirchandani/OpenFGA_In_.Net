# OpenFGA In .Net
Implements **authorization and hierarchical access control using OpenFGA in a .NET application**. It models relationships like **country → state → city → store outlet** and propagates permissions across the hierarchy. Includes helpers to define the authorization model, manage relationships, and check user permissions via the OpenFGA API.
Just like Google Drive’s folder and file access management, granting write access to a parent folder automatically provides access to all child folders.

# OpenFGA Hierarchical Authorization System (.NET)

## Overview

This project implements **hierarchical authorization using OpenFGA** in
a **.NET application**.\
Permissions are assigned at different levels of a geographic hierarchy
and automatically propagate downward.

    Country
       ↓
    State
       ↓
    City
       ↓
    Store Outlet

If a user has permission at a higher level (e.g., Country), they
automatically inherit permissions for all lower levels (State → City →
Store).

------------------------------------------------------------------------

# 1. What is OpenFGA?

OpenFGA is an open‑source authorization system based on **Google
Zanzibar architecture**.

It stores relationships between:

-   **User**
-   **Resource**
-   **Permission**

Relationship format:

    object:objectId#relation@user:userId

Example:

    storeoutlet:174#writer@user:user456

Meaning:

    User456 has writer permission on store outlet 174

------------------------------------------------------------------------

# 2. Project Architecture

                       +---------------------+
                       |   .NET Application  |
                       |                     |
                       |   Controllers/API   |
                       +----------+----------+
                                  |
                                  |
                                  v
                      +-----------------------+
                      |     OpenFGAService    |
                      |                       |
                      |  - Create tuples      |
                      |  - Check permission   |
                      |  - Manage model       |
                      +-----------+-----------+
                                  |
                                  |
                                  v
                  +---------------------------------+
                  | OpenFgaAuthorizationModelHelper |
                  |                                 |
                  | Builds Authorization Model      |
                  +---------------------------------+
                                  |
                                  |
                                  v
                    +-------------------------------+
                    | OpenFgaHierarchyHelper        |
                    |                               |
                    | Builds location hierarchy     |
                    | Country → State → City → Store|
                    +-------------------------------+
                                  |
                                  |
                                  v
                         +----------------+
                         |   OpenFGA DB   |
                         | (Relationships)|
                         +----------------+

------------------------------------------------------------------------

# 3. Authorization Model

Authorization model defines how permissions propagate through the
hierarchy.

Example OpenFGA Model:

``` fga
model
  schema 1.1

type country
  relations
    define writer: [user]

type state
  relations
    define writer: [user] or writer from country

type city
  relations
    define writer: [user] or writer from state

type storeoutlet
  relations
    define writer: [user] or writer from city
```

### Meaning

  Object        Inherits Permission From
  ------------- --------------------------
  Country       Direct assignment
  State         Country
  City          State
  StoreOutlet   City

------------------------------------------------------------------------

# 4. Permission Inheritance Diagram

    User
     │
     │ writer
     ▼
    Country
     │
     ▼
    State
     │
     ▼
    City
     │
     ▼
    StoreOutlet

If permission is assigned at **Country**, the user automatically has
access to all **states, cities, and stores**.

------------------------------------------------------------------------

# 5. Example Hierarchy

    Country: India
       └── State: Maharashtra
              └── City: Pune
                     └── Store: Store101

Hierarchy relationships stored in OpenFGA:

    state:maharashtra#parent@country:india
    city:pune#parent@state:maharashtra
    storeoutlet:store101#parent@city:pune

------------------------------------------------------------------------

# 6. Permission Assignment Example

User:

    user:john

Permission:

    country:india#writer@user:john

Result:

    john can write country:india
    john can write state:maharashtra
    john can write city:pune
    john can write storeoutlet:store101

------------------------------------------------------------------------

# 7. Core Components

## 7.1 OpenFgaAuthorizationModelHelper

Responsible for:

-   Defining authorization schema
-   Declaring resource types
-   Creating permission relations

Main Types:

    user
    country
    state
    city
    storeoutlet

Main Relations:

    writer
    parent

------------------------------------------------------------------------

## 7.2 OpenFgaHierarchyHelper

Defines hierarchical structure.

Example constants:

    Country
    State
    City
    StoreOutlet

Relations:

    Writer
    Parent

------------------------------------------------------------------------

## 7.3 OpenFGAService

Main service communicating with the **OpenFGA API**.

Responsibilities:

-   Create tuples
-   Check user permissions
-   Manage authorization model
-   Connect to OpenFGA server

Example client initialization:

``` csharp
var fgaClient = new OpenFgaApi(new ClientConfiguration
{
    ApiUrl = LocalConfig.OpenFGA.ApiUrl,
});
```

------------------------------------------------------------------------

# 8. Permission Check Flow

    User Request
         |
         v
    API Controller
         |
         v
    OpenFGAService
         |
         v
    OpenFGA Permission Check
         |
         v
    Authorization Decision

Example check:

    Can user456 write storeoutlet:174 ?

OpenFGA evaluates:

    storeoutlet → city → state → country

If permission exists at any level → **access granted**.

------------------------------------------------------------------------

# 9. Example Data Stored in OpenFGA

Hierarchy tuples:

    state:MH#parent@country:India
    city:Pune#parent@state:MH
    storeoutlet:Store100#parent@city:Pune

Permission tuples:

    country:India#writer@user:Admin1
    city:Pune#writer@user:Manager1
    storeoutlet:Store100#writer@user:Staff1

------------------------------------------------------------------------

# 10. Resulting Permissions

  User       Access
  ---------- -----------------
  Admin1     All stores
  Manager1   All Pune stores
  Staff1     Only Store100

------------------------------------------------------------------------

# 11. Benefits of This Architecture

### 1. Hierarchical Permission Inheritance

Assign permission once at a higher level and it propagates
automatically.

### 2. Highly Scalable

OpenFGA can manage millions of relationships efficiently.

### 3. Centralized Authorization

All permissions are stored in a dedicated authorization service.

### 4. Industry Proven

Based on the same model used by:

-   Google Drive
-   YouTube
-   Google Docs

------------------------------------------------------------------------

# 12. Enterprise Use Case

Example global retail system.

    Country Admin
         ↓
    State Manager
         ↓
    City Manager
         ↓
    Store Staff

Permissions:

    Country Admin → Country
    State Manager → State
    City Manager → City
    Store Staff → Store

Permission inheritance automatically grants access to child resources.

------------------------------------------------------------------------

# 13. Full System Diagram

                        +------------------+
                        |  User Login      |
                        +--------+---------+
                                 |
                                 v
                         +---------------+
                         |   API Layer   |
                         +-------+-------+
                                 |
                                 v
                        +------------------+
                        |  OpenFGAService  |
                        +--------+---------+
                                 |
                                 v
                +--------------------------------+
                | AuthorizationModelHelper       |
                +--------------------------------+
                                 |
                                 v
                    +----------------------------+
                    | OpenFgaHierarchyHelper     |
                    +----------------------------+
                                 |
                                 v
                        +----------------+
                        |   OpenFGA DB   |
                        +----------------+

------------------------------------------------------------------------

# 14. Key Concepts

  Concept    Meaning
  ---------- ----------------------
  Tuple      Relationship record
  Object     Resource
  Relation   Permission
  User       Actor
  Model      Authorization schema

------------------------------------------------------------------------

# 15. Explanation (Short)

> We implemented hierarchical authorization using OpenFGA in a .NET
> system.\
> Permissions are assigned at different levels of a location hierarchy
> (country → state → city → store).\
> Using OpenFGA relationship tuples, permissions automatically propagate
> down the hierarchy.\
> The .NET service manages authorization models, hierarchy
> relationships, and permission checks through the OpenFGA API.

------------------------------------------------------------------------

# License

Example documentation for understanding OpenFGA hierarchical
authorization architecture.
