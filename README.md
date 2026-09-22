# Ride-Hailing Backend API

A production-style ride-hailing backend API inspired by platforms such as Uber and inDrive.

The project was built to explore how real-world ride-hailing systems handle authentication, ride management, driver offers, concurrency, and location-based driver discovery.

Rather than building only basic CRUD endpoints, the project focuses on backend engineering concerns such as authorization, data consistency, transactions, concurrency, and high-frequency location data.

---

## Features

### Authentication & Authorization

- User registration and login
- JWT-based authentication
- Role-based authorization
- Passenger and Driver roles
- Protected API endpoints
- Server-side identity derived from JWT claims

### Passenger Features

- Request a ride
- View ride details
- Cancel a ride
- View driver offers
- Accept a driver offer
- Complete ride lifecycle tracking

### Driver Features

- Create driver profile
- View driver profile
- Go Online / Offline
- Update current location
- View nearby pending rides
- Make offers on rides
- Start accepted rides
- Complete rides

### Location Management

- Driver latitude/longitude tracking
- Location validation
- Redis Geospatial indexing
- Nearby-driver/location searches using Redis GEO

### Data Consistency

- PostgreSQL transactions
- Row-level locking
- Protection against conflicting driver offer acceptance
- Ride ownership checks
- Driver assignment checks

---

# Architecture

The application currently follows a layered backend architecture:

```text
                    Client
                      |
                      v
                Controllers
                      |
                      v
                  Services
                      |
                      v
                Repositories
                  /       \
                 /         \
                v           v
        PostgreSQL       Redis GEO
Responsibilities

Controllers

Handle HTTP requests, authentication/authorization, request validation, and HTTP responses.

Services

Contain business logic and coordinate operations between controllers and repositories.

Repositories

Handle database operations and SQL queries.

PostgreSQL

Acts as the primary persistent data store for users, rides, driver profiles, and driver offers.

Redis

Used for frequently changing driver location data and geospatial queries.

Ride Lifecycle

A ride moves through several states:

Pending
   |
   v
Accepted
   |
   v
InProgress
   |
   v
Completed

A ride can also be cancelled where permitted:

Pending ───────> Cancelled
   |
   v
Accepted ──────> Cancelled
Driver Offer Flow

The application uses an inDrive-style driver bidding model.

Passenger
    |
    | Request Ride
    v
Ride (Pending)
    |
    | Drivers submit offers
    v
+----------------------+
| Driver Offer         |
| Driver A - ₦5,000    |
| Driver B - ₦4,500    |
| Driver C - ₦5,500    |
+----------------------+
    |
    | Passenger accepts offer
    v
Ride (Accepted)
    |
    v
Driver starts ride
    |
    v
Ride (InProgress)
    |
    v
Ride (Completed)
Redis Geospatial Architecture

Driver locations are highly dynamic. Drivers can update their positions frequently, while the system needs to perform proximity searches.

Redis GEO is used as a fast geospatial index for driver locations.

Driver App
    |
    | Update Location
    v
ASP.NET Core API
    |
    v
Location Service
    |
    v
Redis GEO

Nearby-driver discovery:

Passenger
    |
    | Pickup coordinates
    v
Ride / Matching Logic
    |
    v
Redis GEO
    |
    | Search radius
    v
Nearby Drivers

Redis GEO operations include concepts such as:

GEOADD — add/update driver coordinates
GEOSEARCH — find drivers within a geographic radius
GEODIST — calculate distance between members
GEOPOS — retrieve stored coordinates

PostgreSQL remains the source of truth for persistent application data.

Concurrency Handling

One of the important backend problems in the system is preventing multiple drivers from accepting the same ride.

For example:

Driver A ──────┐
               |
               v
          Accept Ride #123
               ^
               |
Driver B ──────┘

Both requests could arrive at almost the same time.

The application uses a PostgreSQL transaction with row-level locking when accepting an offer.

Conceptually:

BEGIN TRANSACTION
       |
       v
Lock Ride
       |
       v
Check Ride Status
       |
       v
Accept Selected Offer
       |
       v
Reject Other Pending Offers
       |
       v
Update Ride Status
       |
       v
COMMIT

This prevents conflicting requests from successfully assigning multiple drivers to the same ride.

Authentication Flow

Authentication is implemented using JWT.

User
 |
 | Login
 v
ASP.NET Core
 |
 | Verify credentials
 v
JWT Token
 |
 v
Client
 |
 | Authorization: Bearer <token>
 v
Protected Endpoint
 |
 v
JWT Validation
 |
 v
Role / User Identity

The API derives the authenticated user's ID from the JWT rather than trusting user IDs supplied by the client.

This helps prevent users from attempting to perform operations on behalf of another user.

Database Design

The main entities are:

Users
  |
  +------ DriverProfile
  |
  +------ Rides
             |
             +------ DriverOffers
Users

Stores application users and their roles.

id
full_name
email
password_hash
role
created_at
Driver Profiles

Stores driver and vehicle information.

id
user_id
license_number
vehicle_make
vehicle_model
vehicle_plate_number
status
current_latitude
current_longitude
Rides

Stores passenger ride requests.

id
passenger_id
pickup_latitude
pickup_longitude
destination_latitude
destination_longitude
proposed_fare
ride_type
requested_at
status
Driver Offers

Stores driver offers for rides.

id
ride_id
driver_id
offered_fare
offered_at
status

A unique constraint is used to prevent the same driver from creating duplicate offers for the same ride.

API Endpoints
Authentication
Method	Endpoint	Description
POST	/api/auth/register	Register a user
POST	/api/auth/login	Login and receive JWT
Rides
Method	Endpoint	Description
POST	/api/ride	Create a ride
GET	/api/ride/{id}	Get ride details
PUT	/api/ride/{id}/cancel	Cancel a ride
GET	/api/ride/pending	Get pending rides
GET	/api/ride/nearby	Get nearby pending rides
PUT	/api/ride/{id}/complete	Complete a ride
Driver Profiles
Method	Endpoint	Description
POST	/api/driverprofile	Create driver profile
GET	/api/driverprofile/{id}	Get driver profile
PUT	/api/driverprofile/status	Update driver status
PUT	/api/driverprofile/location	Update driver location
Driver Offers
Method	Endpoint	Description
POST	/api/driveroffer/{rideId}	Create driver offer
GET	/api/driveroffer/{rideId}	Get offers for a ride
PUT	/api/driveroffer/{rideId}/{offerId}/accept	Accept an offer
PUT	/api/driveroffer/{rideId}/start	Start a ride
Technology Stack
Backend
C#
ASP.NET Core
.NET 8
REST API
Database
PostgreSQL
Npgsql
Raw SQL
Caching / Location
Redis
Redis Geospatial Indexing
Security
JWT Authentication
Role-Based Authorization
BCrypt Password Hashing
Development
Postman
Git
GitHub
Project Structure
Backend/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── RideController.cs
│   ├── DriverProfileController.cs
│   └── DriverOfferController.cs
│
├── Services/
│   ├── UserService.cs
│   ├── RideService.cs
│   ├── DriverProfileService.cs
│   └── DriverOfferService.cs
│
├── Repositories/
│   ├── UserRepository.cs
│   ├── RideRepository.cs
│   ├── DriverProfileRepository.cs
│   └── DriverOfferRepository.cs
│
├── Models/
│   ├── User.cs
│   ├── Ride.cs
│   ├── DriverProfile.cs
│   └── DriverOffer.cs
│
├── DTOs/
│
├── Data/
│   └── DbConnection.cs
│
├── Services/
│
├── Program.cs
└── appsettings.json
Getting Started
Prerequisites

Make sure you have:

.NET 8 SDK
PostgreSQL
Redis
Git
Clone the repository
git clone <your-repository-url>
cd <project-directory>
Configure PostgreSQL

Create a PostgreSQL database:

uber_app

Configure your connection string in appsettings.json or, preferably, environment variables/user secrets.

Example:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=uber_app;Username=postgres;Password=YOUR_PASSWORD"
  }
}
Configure JWT

Provide a secure JWT signing key through configuration.

For local development:

{
  "Jwt": {
    "Key": "your-super-secret-key"
  }
}

Do not commit real production secrets to source control.

Running Redis

Make sure Redis is running locally.

Example:

redis-server

You can verify the connection using:

redis-cli ping

Expected response:

PONG
Run the API
dotnet restore
dotnet run

The API will start on the configured HTTP/HTTPS ports.

You can use Postman or another API client to test the endpoints.

Example Ride Flow
1. Register
POST /api/auth/register
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "password123",
  "role": "Passenger"
}
2. Login
POST /api/auth/login

Receive the JWT.

3. Create Ride
POST /api/ride
Authorization: Bearer <token>
{
  "pickupLatitude": 6.5244,
  "pickupLongitude": 3.3792,
  "destinationLatitude": 6.6018,
  "destinationLongitude": 3.3515,
  "proposedFare": 5000,
  "rideType": "Standard"
}
4. Driver Makes Offer
POST /api/driveroffer/{rideId}
Authorization: Bearer <driver-token>
{
  "offeredFare": 4500
}
5. Passenger Accepts Offer
PUT /api/driveroffer/{rideId}/{offerId}/accept
Authorization: Bearer <passenger-token>
6. Driver Starts Ride
PUT /api/driveroffer/{rideId}/start
Authorization: Bearer <driver-token>
7. Driver Completes Ride
PUT /api/ride/{rideId}/complete
Authorization: Bearer <driver-token>
Engineering Decisions
Why PostgreSQL?

PostgreSQL provides reliable persistent storage and strong transactional guarantees for data such as:

Users
Rides
Driver profiles
Driver offers
Why raw SQL?

The project uses Npgsql and raw SQL to gain direct experience with:

SQL queries
Transactions
Row-level locking
Database constraints
Query behavior
Why Redis?

Driver locations are frequently changing data, and nearby-driver discovery is a location-based operation.

Redis provides a specialized geospatial index that can efficiently perform proximity searches without making PostgreSQL handle every location lookup.

Why JWT?

JWT allows the API to authenticate requests without maintaining server-side session state for every request.

Current Architecture vs Future Architecture

The current system is a layered application:

                    ┌──────────────┐
                    │    Client    │
                    └──────┬───────┘
                           │
                           v
                    ┌──────────────┐
                    │ Controllers  │
                    └──────┬───────┘
                           │
                           v
                    ┌──────────────┐
                    │   Services   │
                    └──────┬───────┘
                           │
                    ┌──────┴───────┐
                    ↓              ↓
             PostgreSQL        Redis GEO

As the project evolves, possible improvements include:

                 API Gateway
                      |
        ┌─────────────┼─────────────┐
        ↓             ↓             ↓
   Ride Service  Location Service  Matching Service
        |             |             |
        ↓             ↓             ↓
   PostgreSQL      Redis GEO      Redis GEO
        |
        └────────── Kafka ──────────┘

These are planned architectural improvements rather than features currently represented as fully implemented.