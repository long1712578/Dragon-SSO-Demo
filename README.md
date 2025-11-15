# Dragon SSO Demo

## Overview
This repository contains a complete microservices-based Single Sign-On (SSO) solution implemented using .NET 8 and ABP Framework 9.0.2.

### Services included:
1. **Identity Service**: Handles authentication and authorization using OpenIddict.
2. **Office Service**: Manages office resources and related operations.
3. **HRM Service**: Manages human resource management tasks.
4. **Payroll Service**: Manages payroll processing.
5. **API Gateway**: Acts as a gateway for all services using Ocelot.

### Getting Started
To get started, clone the repository and run the provided Docker Compose file for setting up services.

### Directory Structure
- **src/services/identity** - Contains IdentityService API code.
- **src/services/office** - Contains OfficeService API code.
- **src/services/hrm** - Contains HRMService API code.
- **src/services/payroll** - Contains PayrollService API code.
- **src/gateway** - Contains API Gateway code.
- **src/shared** - Contains Shared.Hosting code.
- **docker/** - Contains Docker configurations.
- **scripts/** - Contains migration scripts and other scripts.

### Requirements
- .NET 8 SDK
- Docker

### Usage
1. Run `docker-compose up` to start the application.
2. Access services via the API Gateway.

### Database Migrations
Run PowerShell scripts located in the `scripts/` directory to apply any database migrations.

### License
This project is licensed under the MIT License.