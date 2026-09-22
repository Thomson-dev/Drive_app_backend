# Drive App API Documentation

Comprehensive API documentation for the Drive App backend.

- **Base URL:** `http://localhost:5000`
- **Interactive Swagger UI:** `http://localhost:5000/swagger`
- **Authentication Scheme:** `Bearer <JWT_TOKEN>` in the `Authorization` header

---

## Table of Contents
1. [Authentication (`/auth`)](#1-authentication-auth)
   - [Register User](#post-authregister)
   - [Login User](#post-authlogin)
2. [Driver Profiles (`/drivers`)](#2-driver-profiles-drivers)
   - [Create Driver Profile](#post-drivers)
   - [Get Driver Profile](#get-driversid)
   - [Update Driver Status](#patch-driversidstatus)
   - [Update Driver Location](#patch-driversidlocation)
3. [Rides (`/rides`)](#3-rides-rides)
   - [Create Ride Request](#post-rides)
   - [Get Ride Details](#get-ridesid)
   - [Get Pending Rides](#get-ridespending)
   - [Cancel Ride](#patch-ridesidcancel)
   - [Start Ride](#patch-ridesrideidstart)
   - [Complete Ride](#patch-ridesrideidcomplete)
4. [Driver Offers (`/rides/{rideId}/offers`)](#4-driver-offers-ridesrideidoffers)
   - [Create Driver Offer](#post-ridesrideidoffers)
   - [Get Ride Offers](#get-ridesrideidoffers)
   - [Accept Driver Offer](#patch-ridesrideidoffersofferidaccept)
5. [End-to-End Testing Lifecycle](#5-end-to-end-testing-lifecycle)
6. [Testing Tools Available](#6-testing-tools-available)

---

## 1. Authentication (`/auth`)

### `POST /auth/register`
Creates a new user account with role `Passenger` or `Driver`.

- **Access:** Public
- **Headers:** `Content-Type: application/json`
- **Request Body:**
```json
{
  "fullName": "Alice Johnson",
  "email": "alice@example.com",
  "password": "Password123!",
  "role": "Passenger"
}
```
> **Note:** `role` must be `"Passenger"` or `"Driver"`.

- **Response `200 OK`:**
```json
{
  "id": "e44b82bc-9d05-4f36-932d-20da0829bfef",
  "message": "User registered successfully"
}
```

---

### `POST /auth/login`
Authenticates a user and generates a JWT access token.

- **Access:** Public
- **Headers:** `Content-Type: application/json`
- **Request Body:**
```json
{
  "email": "alice@example.com",
  "password": "Password123!"
}
```
- **Response `200 OK`:**
```json
{
  "id": "e44b82bc-9d05-4f36-932d-20da0829bfef",
  "fullName": "Alice Johnson",
  "email": "alice@example.com",
  "role": "Passenger",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```
- **Response `401 Unauthorized`:**
```json
{
  "message": "Invalid email or password."
}
```

---

## 2. Driver Profiles (`/drivers`)

### `POST /drivers`
Registers vehicle information and initializes a profile for the authenticated driver.

- **Access:** `Driver` role only
- **Headers:**
  - `Authorization: Bearer <driver_token>`
  - `Content-Type: application/json`
- **Request Body:**
```json
{
  "licenseNumber": "DL-987654321",
  "vehicleMake": "Toyota",
  "vehicleModel": "Corolla",
  "vehiclePlateNumber": "LAG-123-XY"
}
```
- **Response `201 Created`:**
```json
{
  "id": "787c88b0-84eb-4fa6-84d5-8f6cfbc5fa3b",
  "userId": "d7a4ebad-8d77-4bf7-9f6b-d36474fb246b",
  "licenseNumber": "DL-987654321",
  "vehicleMake": "Toyota",
  "vehicleModel": "Corolla",
  "vehiclePlateNumber": "LAG-123-XY",
  "status": "Offline",
  "currentLatitude": null,
  "currentLongitude": null
}
```

---

### `GET /drivers/{id}`
Retrieves a driver's profile information by ID.

- **Access:** Any authenticated user
- **Headers:** `Authorization: Bearer <token>`
- **Response `200 OK`:** Driver profile object
- **Response `404 Not Found`:** Profile does not exist

---

### `PATCH /drivers/{id}/status`
Sets the driver's availability status. Driver must be `"Online"` to discover pending rides.

- **Access:** `Driver` role (must own the profile)
- **Headers:**
  - `Authorization: Bearer <driver_token>`
  - `Content-Type: application/json`
- **Request Body:** *(raw JSON string)*
```json
"Online"
```
*(Accepted values: `"Online"`, `"Offline"`)*

- **Response `200 OK`:**
```json
{
  "message": "Driver is now Online"
}
```

---

### `PATCH /drivers/{id}/location`
Updates the driver's current GPS location coordinates.

- **Access:** `Driver` role (must own the profile)

### `GET /drivers/nearby`
Finds drivers stored in Redis within a radius of a location, ordered by distance.

- **Access:** Any authenticated user
- **Headers:** `Authorization: Bearer <token>`
- **Query Parameters:** `latitude`, `longitude`, and optional `radiusKm` (defaults to `5`)
- **Example:** `GET /drivers/nearby?latitude=6.5244&longitude=3.3792&radiusKm=5`
- **Response `200 OK`:**
```json
[
  {
    "driverId": "787c88b0-84eb-4fa6-84d5-8f6cfbc5fa3b",
    "distanceKm": 0.42
  }
]
```
- **Headers:**
  - `Authorization: Bearer <driver_token>`
  - `Content-Type: application/json`
- **Request Body:**
```json
{
  "latitude": 6.5244,
  "longitude": 3.3792
}
```
- **Response `200 OK`:**
```json
{
  "message": "Driver location updated successfully"
}
```

---

## 3. Rides (`/rides`)

### `POST /rides`
Creates a new ride request initiated by a passenger.

- **Access:** `Passenger` role only
- **Headers:**
  - `Authorization: Bearer <passenger_token>`
  - `Content-Type: application/json`
- **Request Body:**
```json
{
  "pickupLatitude": 6.5244,
  "pickupLongitude": 3.3792,
  "destinationLatitude": 6.6018,
  "destinationLongitude": 3.3515,
  "proposedFare": 2500.00,
  "rideType": "Economy"
}
```
- **Response `201 Created`:**
```json
{
  "id": "fa987c2b-2fe3-4416-92cb-05bb0f496739",
  "passengerId": "e44b82bc-9d05-4f36-932d-20da0829bfef",
  "pickupLatitude": 6.5244,
  "pickupLongitude": 3.3792,
  "destinationLatitude": 6.6018,
  "destinationLongitude": 3.3515,
  "proposedFare": 2500.00,
  "rideType": "Economy",
  "requestedAt": "2026-09-22T09:40:00Z",
  "status": "Pending"
}
```

---

### `GET /rides/{id}`
Retrieves current details of a specific ride.

- **Access:** Any authenticated user
- **Headers:** `Authorization: Bearer <token>`
- **Response `200 OK`:** Ride object
- **Response `404 Not Found`:** Ride not found

---

### `GET /rides/pending`
Lists all active rides in `Pending` status. Driver must be `"Online"` to access this endpoint.

- **Access:** `Driver` role only (Driver status must be `"Online"`)
- **Headers:** `Authorization: Bearer <driver_token>`
- **Response `200 OK`:**
```json
[
  {
    "id": "fa987c2b-2fe3-4416-92cb-05bb0f496739",
    "passengerId": "e44b82bc-9d05-4f36-932d-20da0829bfef",
    "pickupLatitude": 6.5244,
    "pickupLongitude": 3.3792,
    "destinationLatitude": 6.6018,
    "destinationLongitude": 3.3515,
    "proposedFare": 2500.00,
    "rideType": "Economy",
    "requestedAt": "2026-09-22T09:40:00Z",
    "status": "Pending"
  }
]
```
- **Response `400 Bad Request`:** `{"message": "Driver must be online to view available rides."}`

---

### `PATCH /rides/{id}/cancel`
Cancels a ride. Only valid when status is `Pending` or `Accepted`.

- **Access:** `Passenger` role (must own the ride)
- **Headers:** `Authorization: Bearer <passenger_token>`
- **Response `200 OK`:**
```json
{
  "message": "Ride cancelled successfully"
}
```

---

### `PATCH /rides/{rideId}/start`
Starts a trip after an offer has been accepted. Transitions status to `InProgress`.

- **Access:** `Driver` role (must be the driver whose offer was accepted)
- **Headers:** `Authorization: Bearer <driver_token>`
- **Response `200 OK`:**
```json
{
  "message": "Ride started successfully"
}
```

---

### `PATCH /rides/{rideId}/complete`
Finishes an in-progress trip. Transitions status to `Completed`.

- **Access:** `Driver` role (must be the driver with the active trip)
- **Headers:** `Authorization: Bearer <driver_token>`
- **Response `200 OK`:**
```json
{
  "message": "Ride completed successfully"
}
```

---

## 4. Driver Offers (`/rides/{rideId}/offers`)

### `POST /rides/{rideId}/offers`
Driver places an offer or counter-offer for a pending ride.

- **Access:** `Driver` role
- **Headers:**
  - `Authorization: Bearer <driver_token>`
  - `Content-Type: application/json`
- **Request Body:**
```json
{
  "offeredFare": 2800.00
}
```
- **Response `200 OK`:**
```json
{
  "id": "c138b368-45e0-4ad8-8cf2-ec067cfd8b49"
}
```

---

### `GET /rides/{rideId}/offers`
Lists all driver offers received for a specific ride.

- **Access:** `Passenger` role (must be the passenger who requested the ride)
- **Headers:** `Authorization: Bearer <passenger_token>`
- **Response `200 OK`:**
```json
[
  {
    "id": "c138b368-45e0-4ad8-8cf2-ec067cfd8b49",
    "rideId": "fa987c2b-2fe3-4416-92cb-05bb0f496739",
    "driverId": "787c88b0-84eb-4fa6-84d5-8f6cfbc5fa3b",
    "offeredFare": 2800.00,
    "offeredAt": "2026-09-22T09:41:00Z",
    "status": "Pending"
  }
]
```

---

### `PATCH /rides/{rideId}/offers/{offerId}/accept`
Passenger accepts a driver's offer. Transitions the offer and ride status to `Accepted`.

- **Access:** `Passenger` role (must own the ride)
- **Headers:** `Authorization: Bearer <passenger_token>`
- **Response `200 OK`:**
```json
{
  "message": "Offer accepted successfully"
}
```

---

## 5. End-to-End Testing Lifecycle

To test all endpoints smoothly, follow this logical flow:

```
[1. REGISTER PASSENGER] ──> [2. LOGIN PASSENGER] (Get Passenger Token)
[3. REGISTER DRIVER]    ──> [4. LOGIN DRIVER]    (Get Driver Token)
                                   │
                                   ▼
                        [5. CREATE DRIVER PROFILE]
                                   │
                                   ▼
                        [6. SET STATUS TO "Online"]
                                   │
                                   ▼
[7. PASSENGER CREATES RIDE] <──────┼──────────────────────────────┐
            │                      ▼                              │
            │           [8. DRIVER CHECKS PENDING RIDES]          │
            │                      │                              │
            │                      ▼                              │
            │           [9. DRIVER SUBMITS OFFER]                 │
            ▼                      │                              │
[10. PASSENGER VIEWS OFFERS] <─────┘                              │
            │                                                     │
            ▼                                                     │
[11. PASSENGER ACCEPTS OFFER]                                     │
            │                                                     │
            ▼                                            [CANCEL RIDE]
[12. DRIVER STARTS RIDE] (InProgress)                  (Optional branch)
            │
            ▼
[13. DRIVER COMPLETES RIDE] (Completed)
```

---

## 6. Testing Tools Available

### Option 1: Swagger UI (Browser)
Visit **`http://localhost:5000/swagger`** while the backend is running.
Click the green **Authorize** button and paste your JWT token to authorize calls.

### Option 2: VS Code REST Client (`backend-api/DriveApp.http`)
Open `backend-api/DriveApp.http` and click **"Send Request"** above any endpoint. Responses automatically flow into subsequent requests.

### Option 3: Automated Bash Script (`backend-api/test_endpoints.sh`)
Execute all 13 endpoints in sequence with verification:
```bash
cd backend-api
./test_endpoints.sh
```
