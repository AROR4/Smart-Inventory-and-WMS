Smart Inventory & Warehouse Management
Objective:
Develop a complete enterprise-grade application using layered architecture with
PostgreSQL, ASP.NET Core Web API, and Angular frontend.
Core Modules:
• Inventory Tracking
• Supplier Management
• Purchase Orders
• Barcode Management
• Warehouse Transfers

Key Features:
• Low-stock alerts
• Multi-warehouse support
• Search & pagination

Suggested Architecture:
Angular Frontend → ASP.NET Core Web API → Service Layer → Repository Layer →
PostgreSQL Database
Azure Integration:
• Azure Service Bus
• Azure Blob Storage

Student Deliverables:
• Requirement Document
• ER Diagram
• API Documentation
• Angular Screens
• Authentication & Authorization
• CRUD APIs
• Search & Pagination
• Deployment Steps

Phase 2 Enhancements:
• Microservices
• Redis caching

######

Objectives:

Develop an enterprise-grade inventory and warehouse management platform using:

Frontend: Angular
Backend: ASP.NET Core Web API
Database: PostgreSQL
Cloud Services: Microsoft Azure

The system should support:

Inventory management
Multi-warehouse operations
Supplier management
Purchase order workflow
Barcode management
Warehouse stock transfers
Authentication & authorization
Search and pagination
Low-stock alerts

Modules to be implemented:

1 Authentication & Authorization Module

Features
    User Registration
    User Login
    JWT Authentication
    Role-Based Authorization
    Domain-Based Authorization

Roles

Admin
    Full access
    Manage users
    Manage warehouses
    Create & approve purchase orders
    Configure system
    Manage suppliers

Warehouse Manager
    Manage inventory
    Approve transfers
    View reports

Inventory Staff
    Update stock
    Scan barcodes


2 Inventory Tracking Module

Features
    Add products
    Update stock
    Delete products
    View inventory
    Stock adjustment
    Inventory history

Product Information
    SKU
    Product Name
    Description
    Category
    Quantity
    Reorder Level
    Barcode
    Warehouse
    Price

3 Supplier Management Module

Features
    Add supplier
    Edit supplier
    Delete supplier
    Supplier contact details
    Supplier history

Supplier Fields
    Supplier Name
    Contact Person
    Email
    Phone
    Address
    GST Number
    Status

4 Purchase Order Module

Features
    Create purchase order
    Receive inventory
    Update stock after receiving
    Track order status

Purchase Order Status
    Pending
    Approved
    Ordered
    Received
    Cancelled

5 Barcode Management Module

Features
    Generate barcode
    Upload barcode image
    Scan barcode
    Search product by barcode

6 Warehouse Management Module

Features
    Create warehouse
    Assign products to warehouse
    Track warehouse stock
    Warehouse transfer

Warehouse Information
    Warehouse Name
    Location
    Capacity
    Manager


7 Low Stock Alert Module

Features
    Detect low stock
    Send alerts
    Notification queue

