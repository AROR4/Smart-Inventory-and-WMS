# Smart Inventory & Warehouse Management System

A role-based Warehouse & Inventory Management System built with **ASP.NET Core**, **Entity Framework Core**, and **PostgreSQL**. The system manages inventory operations, warehouse transfers, purchase orders, warehouse tasks, stock movements, and low-stock monitoring while maintaining auditability, concurrency control, and secure access management.

---

## Overview

---

## Key Features

### Inventory Management
- Add, Remove & Adjust Stock
- Search, Filtering & Pagination
- Capacity & Storage Type Validation

### Purchase Order Management
- Create, Approve, Receive & Complete Orders
- Supplier Integration
- Automatic Warehouse Task Generation

### Warehouse Transfer Management
- Create, Approve, Transit, Receive & Complete Transfers
- Automatic Source & Destination Task Generation

### Warehouse Task Management
- Store & Retrieve Inventory Tasks
- Start, Complete & Track Tasks
- Task Assignment

### Low Stock Monitoring
- Automatic Alert Creation & Resolution
- Reorder Level Monitoring

### Stock Movement Tracking
Every inventory movement is recorded:

| Movement Type       |
|---------------------|
| Stock In            |
| Stock Out           |
| Inventory Adjustment|
| Transfer Movement   |
| Purchase Receipt    |

### User & Role Management

| Role               |
|--------------------|
| Admin              |
| Warehouse Owner    |
| Warehouse Manager  |
| Warehouse Staff    |
| Supplier           |

### Security
- JWT Authentication
- Password Setup via Email
- Role-Based Authorization
- Warehouse-Based Data Isolation
- Ownership Validation
- Token Validation & Expiry Handling

---

## Core Business Flows

### 1. Purchase Order Flow

```
Purchase Order Created
        ↓
Approved
        ↓
Supplier Ships Goods
        ↓
Goods Received
        ↓
Store Inventory Task Created
        ↓
Task Completed
        ↓
Inventory Updated
        ↓
Purchase Order Completed
```

### 2. Warehouse Transfer Flow

```
Transfer Created
        ↓
Approved
        ↓
Retrieve Inventory Task Created (Source Warehouse)
        ↓
Task Completed
        ↓
Transfer In Transit
        ↓
Received At Destination
        ↓
Store Inventory Task Created (Destination Warehouse)
        ↓
Task Completed
        ↓
Transfer Completed
```

### 3. Customer Dispatch Flow

```
Dispatch Order Created
        ↓
Retrieve Inventory Task Created
        ↓
Task Completed
        ↓
Inventory Reduced
        ↓
Order Dispatched
```

---

## Warehouse Task-Driven Architecture

The entire system revolves around Warehouse Tasks. Only two task types exist:

| Task Type          | Purpose                       |
|--------------------|-------------------------------|
| Store Inventory    | Adds stock into a warehouse   |
| Retrieve Inventory | Removes stock from a warehouse|

**Each task contains:**

```
Reference Type
Reference Id
Warehouse
Products
Quantity
Assigned User
Status
```

**Reference Types:**

```
PurchaseOrder
WarehouseTransfer
DispatchOrder
```

**When a task is completed, the system automatically:**

- ✅ Updates Inventory
- ✅ Records Stock Movement
- ✅ Updates Related Document Status
- ✅ Checks Low Stock Alerts
- ✅ Updates Warehouse Capacity

---

## Inventory Storage Types

Products can only be stored in compatible warehouse types:

| Storage Type                   | Example Products         |
|-------------------------------|--------------------------|
| Dry Storage                   | General goods            |
| Cold Storage                  | Frozen food              |
| Temperature Controlled Storage| Vaccines                 |
| Hazardous Storage             | Chemical solvents        |
| Fragile Storage               | Glass products           |

---

## Concurrency Control

The system implements **optimistic concurrency control** using EF Core `RowVersion`.

**Protected Operations:**
- Add / Remove / Adjust Stock
- Purchase Order Completion
- Warehouse Transfer Completion

**Prevents:**
- Lost Updates
- Negative Stock
- Capacity Over-Allocation
- Simultaneous Modification Issues

---

## Audit Trail

All major entities maintain:

```
CreatedAt
CreatedByUserId
UpdatedAt
UpdatedByUserId
```

This provides traceability, accountability, change history, and user tracking.

---

## Warehouse Security Model

### Admin
Unrestricted system-wide access.

### Warehouse Owner
Access limited to:
- Owned Warehouses
- Warehouse Reports
- Warehouse Inventory
- Warehouse Details

### Warehouse Manager
Access limited to:
- Inventory of assigned warehouse
- Tasks of assigned warehouse
- Purchase Orders of assigned warehouse
- Transfers involving assigned warehouse

---

## Database Modules

### Master Data
- Categories, Companies, Products, Suppliers
- Warehouses, Roles, Users

### Operational Data
- Inventories
- Purchase Orders & Items
- Warehouse Transfers & Items
- Warehouse Tasks & Items
- Stock Movements
- Low Stock Alerts

---

## Pagination Support

All major modules support pagination.

**Request:**
```http
GET /api/products?pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 42,
  "totalPages": 5,
  "data": [...]
}
```

**Supported Modules:** Products, Purchase Orders, Warehouse Transfers, Warehouse Tasks, Inventory, Users

---

## Logging

Implemented using **ASP.NET Core Logging**.

Logged events include: User Login, Failed Login Attempts, Purchase Order & Transfer Operations, Task Operations, Inventory Updates, Security Events, and Unhandled Exceptions.

---

## Technology Stack

| Layer        | Technology                            |
|--------------|---------------------------------------|
| Backend      | ASP.NET Core                          |
| ORM          | Entity Framework Core                 |
| Mapping      | AutoMapper                            |
| Auth         | JWT Authentication                    |
| Database     | PostgreSQL                            |
| Testing      | NUnit, In-Memory Database Testing     |

### Architecture

```
API Layer
     ↓
Business Layer
     ↓
Repository Layer
     ↓
PostgreSQL Database
```

---

## Testing

- Unit Tests
- Service Layer Tests
- Authorization Tests
- Validation Tests
- Concurrency Tests

**Code Coverage: ~99.5%**

---

## Future Enhancements

- [ ] Dispatch Order Module
- [ ] Dashboard & Analytics
- [ ] Email Notifications
- [ ] Warehouse Performance Reports
- [ ] Mobile Application
- [ ] Barcode Scanning
- [ ] QR-Based Inventory Tracking

---

## Project Highlights

| Highlight                        |
|----------------------------------|
| Role-Based Access Control        |
| Warehouse-Based Data Isolation   |
| Task-Driven Inventory Operations |
| Automatic Status Management      |
| Low Stock Monitoring             |
| Concurrency Handling             |
| Audit Trail Support              |
| High Test Coverage (~99.5%)      |
| Production-Style Architecture    |

