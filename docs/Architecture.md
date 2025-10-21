# EvvaAgent - Modern Modular Architecture

## Overview

EvvaAgent implements a sophisticated modular architecture based on Domain-Driven Design (DDD) principles, Clean Architecture patterns, and modern .NET practices. The system is designed for high extensibility, maintainability, and cross-platform deployment.

## Architecture Principles

### 1. Separation of Concerns
- **Core**: Contains abstractions and command orchestration
- **Modules**: Domain-specific business logic
- **Infrastructure**: Cross-cutting technical concerns
- **Domain**: Pure business entities and rules

### 2. Dependency Inversion
- All dependencies flow inward toward abstractions
- Infrastructure depends on domain, not vice versa
- Modules are pluggable through interfaces

### 3. Single Responsibility
- Each layer has a specific, well-defined purpose
- Services are focused on single business capabilities
- Clear boundaries between technical and business concerns

## Detailed Folder Structure

```
evva-agent/
├── Core/                           # 🏗️ Core System Architecture
│   ├── Abstractions/              # Base interfaces (IEvvaModule, IEvvaResource)
│   ├── Commands/                  # ModularCommandService - Command orchestration
│   └── Extensions/                # ServiceCollectionExtensions - DI registration
│
├── Modules/                       # 🔌 Feature Modules (Pluggable)
│   └── Nginx/                     # Nginx management module
│       ├── Domain/               # NginxModels, configurations, DTOs
│       ├── Services/             # NginxService - Business logic
│       ├── Resources/            # NginxResource - Command implementations
│       └── NginxModule.cs        # Module registration and routing
│
├── Infrastructure/               # 🛠️ Technical Infrastructure
│   ├── Communication/           # CoreHubService - SignalR integration
│   ├── Execution/              # CommandExecutorService - OS command execution
│   ├── Deployment/             # DeploymentService - Workflow orchestration
│   ├── Metrics/                # MetricsService - System monitoring
│   └── Data/                   # Repository implementations
│
├── Domain/                      # 🏛️ Domain Layer
│   ├── Repositories/           # Repository interfaces
│   ├── Entities/              # Core business entities
│   │   ├── Configuration.cs   # System configuration
│   │   ├── Project.cs        # Deployment projects
│   │   ├── Log.cs           # Audit logging
│   │   └── CollectMetric.cs # Metrics data
│   └── DTOs/                 # Data transfer objects
│
├── Data/                       # 💾 Data Access Layer
│   ├── AgentDbContext.cs      # Entity Framework context
│   └── Migrations/           # Database migrations
│
├── Workers/                    # ⚙️ Background Services
│   └── MetricsCollectorWorker.cs # Periodic metrics collection
│
├── Services/                   # 🔧 Application Services
│   └── ServiceManager.cs      # Legacy service management
│
├── DTOs/                      # 📦 Data Transfer Objects
│   └── HostMetricsDto.cs     # System metrics DTO
│
└── docs/                      # 📚 Documentation
    ├── Architecture.md       # This file
    ├── ModuleSystem.md      # Module development guide
    ├── Infrastructure.md    # Infrastructure services
    ├── Classes.md          # API reference
    ├── NginxModule.md      # Nginx module documentation
    └── EvvaCommands.md     # Command system reference
```

## Architecture Layers

### 1. Core Layer 🏗️
**Responsibility**: System abstractions and command orchestration

**Key Components**:
- `IEvvaModule`: Base interface for all feature modules
- `IEvvaResource`: Interface for command resource implementations
- `ModularCommandService`: Central command router and orchestrator
- `ServiceCollectionExtensions`: Dependency injection configuration

**Design Patterns**:
- **Command Pattern**: For executing operations
- **Strategy Pattern**: For module selection
- **Factory Pattern**: For module instantiation

### 2. Modules Layer 🔌
**Responsibility**: Domain-specific business capabilities

**Module Structure** (Example: Nginx):
```
Nginx/
├── Domain/
│   ├── NginxServerConfig.cs      # Configuration models
│   ├── NginxStatus.cs           # Status models
│   ├── ReverseProxyConfig.cs    # Proxy configuration
│   └── StaticSiteConfig.cs      # Static site configuration
├── Services/
│   └── NginxService.cs          # Core business logic
├── Resources/
│   └── NginxResource.cs         # Command implementations
└── NginxModule.cs               # Module registration
```

**Module Capabilities**:
- Self-contained business logic
- Independent deployment
- Configurable through DI
- Command discovery and execution

### 3. Infrastructure Layer 🛠️
**Responsibility**: Technical cross-cutting concerns

**Services**:
- **Communication**: SignalR hub management, real-time messaging
- **Execution**: Cross-platform command execution
- **Deployment**: Git-based deployment workflows
- **Metrics**: System monitoring and data collection
- **Data**: Repository pattern implementations

**Infrastructure Patterns**:
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Observer Pattern**: Event-driven communication

### 4. Domain Layer 🏛️
**Responsibility**: Pure business logic and entities

**Entities**:
- `Configuration`: System configuration settings
- `Project`: Deployment project definitions
- `Log`: Audit and operational logging
- `CollectMetric`: Performance and system metrics

**Repository Interfaces**:
- `IConfigurationRepository`
- `IProjectRepository`
- `ILogRepository`
- `ICollectMetricRepository`

### 5. Data Layer 💾
**Responsibility**: Data persistence and migrations

**Components**:
- `AgentDbContext`: Entity Framework Core context
- Repository implementations
- Database migrations
- SQLite provider configuration

## Command Flow Architecture

### High-Level Flow
```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   Client    │───▶│  CoreHubService  │───▶│ ModularCommandService│
│ (EvvaCore)  │    │   (SignalR)      │    │   (Orchestrator)    │
└─────────────┘    └──────────────────┘    └─────────────────────┘
                                                       │
                                                       ▼
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   Service   │◀───│    Resource      │◀───│      Module         │
│ (Business)  │    │ (Implementation) │    │   (Router)          │
└─────────────┘    └──────────────────┘    └─────────────────────┘
```

### Detailed Command Processing

1. **Command Reception**
   - SignalR client sends command from EvvaCore
   - CoreHubService receives and validates
   - Authentication and authorization checks

2. **Command Parsing**
   - Parse command format: `evva.{module}.{action}.{subaction}`
   - Extract module name, resource, and method
   - Validate command structure

3. **Module Resolution**
   - ModularCommandService locates target module
   - Dependency injection resolves module instance
   - Module validates command availability

4. **Resource Execution**
   - Module routes to appropriate resource
   - Resource deserializes parameters
   - Business service performs operation

5. **Response Handling**
   - Service returns result object
   - Resource formats response
   - CoreHubService sends response to client

### Error Handling Flow
```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   Error     │───▶│   Logging        │───▶│   Notification      │
│ Detection   │    │  (Structured)    │    │   (SignalR)         │
└─────────────┘    └──────────────────┘    └─────────────────────┘
       │                     │                        │
       ▼                     ▼                        ▼
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│  Rollback   │    │   Metrics        │    │   Client Error      │
│ Operations  │    │  Collection      │    │   Response          │
└─────────────┘    └──────────────────┘    └─────────────────────┘
```

## Design Principles & Patterns

### SOLID Principles Implementation

#### Single Responsibility Principle (SRP)
- **Modules**: Each handles one business domain (Nginx, Docker, etc.)
- **Services**: Focused on specific business operations
- **Resources**: Handle only command execution logic
- **Repositories**: Manage only data access for specific entities

#### Open/Closed Principle (OCP)
- **Module System**: Add new modules without modifying core
- **Command System**: Extend commands without changing orchestrator
- **Infrastructure**: Add new services through interfaces

#### Liskov Substitution Principle (LSP)
- **IEvvaModule**: All modules are interchangeable
- **IEvvaResource**: All resources follow same contract
- **Repository Interfaces**: Implementations are substitutable

#### Interface Segregation Principle (ISP)
- **Focused Interfaces**: Small, specific interfaces
- **Role-based**: Interfaces match client needs
- **No Fat Interfaces**: Clients don't depend on unused methods

#### Dependency Inversion Principle (DIP)
- **Abstractions**: High-level modules depend on interfaces
- **Dependency Injection**: All dependencies injected
- **Inversion of Control**: Framework manages object lifecycle

### Architectural Patterns

#### Domain-Driven Design (DDD)
- **Bounded Contexts**: Each module represents a bounded context
- **Entities**: Rich domain objects with behavior
- **Value Objects**: Immutable configuration objects
- **Domain Services**: Complex business operations

#### Clean Architecture
- **Dependency Direction**: Dependencies point inward
- **Layer Isolation**: Each layer has specific responsibilities
- **Framework Independence**: Business logic independent of frameworks

#### Command Query Responsibility Segregation (CQRS)
- **Command Handlers**: Modify system state
- **Query Handlers**: Read system state
- **Separation**: Commands and queries use different models

#### Event-Driven Architecture
- **Domain Events**: Business events trigger side effects
- **Event Handlers**: Respond to system events
- **Loose Coupling**: Components communicate through events

### Cross-Cutting Concerns

#### Logging
- **Structured Logging**: JSON-formatted logs with context
- **Log Levels**: Appropriate levels for different scenarios
- **Correlation IDs**: Track requests across components

#### Error Handling
- **Global Exception Handling**: Centralized error processing
- **Graceful Degradation**: System continues operating on errors
- **Error Recovery**: Automatic retry and rollback mechanisms

#### Security
- **Authentication**: SignalR connection authentication
- **Authorization**: Command-level access control
- **Input Validation**: Parameter validation and sanitization

#### Performance
- **Async/Await**: Non-blocking operations
- **Connection Pooling**: Efficient database connections
- **Caching**: Strategic caching of frequently accessed data

#### Monitoring
- **Health Checks**: System component health monitoring
- **Metrics Collection**: Performance and usage metrics
- **Alerting**: Proactive issue notification

## Technology Stack

### Core Technologies
- **.NET 9**: Latest .NET platform
- **ASP.NET Core**: Web framework and hosting
- **Entity Framework Core**: ORM and data access
- **SQLite**: Embedded database
- **SignalR**: Real-time communication

### Development Tools
- **Dependency Injection**: Built-in DI container
- **Configuration**: JSON-based configuration
- **Logging**: Microsoft.Extensions.Logging
- **Testing**: xUnit, Moq, FluentAssertions

### Deployment
- **Cross-Platform**: Windows, Linux, macOS
- **Self-Contained**: Single executable deployment
- **Docker**: Containerized deployment option
- **Windows Service**: Native Windows service support

## Scalability Considerations

### Horizontal Scaling
- **Stateless Design**: No server-side session state
- **Database Per Service**: Each module can have its own database
- **Load Balancing**: Multiple agent instances

### Vertical Scaling
- **Async Operations**: Efficient resource utilization
- **Connection Pooling**: Optimized database connections
- **Memory Management**: Proper disposal patterns

### Performance Optimization
- **Lazy Loading**: Load resources only when needed
- **Caching Strategy**: Cache frequently accessed data
- **Background Processing**: Long-running operations in background

## Security Architecture

### Authentication & Authorization
- **SignalR Authentication**: Secure hub connections
- **API Key Authentication**: REST API security
- **Role-Based Access**: Command-level permissions

### Data Protection
- **Encryption at Rest**: Sensitive configuration data
- **Secure Communication**: HTTPS/WSS protocols
- **Input Sanitization**: Prevent injection attacks

### Audit & Compliance
- **Audit Logging**: All operations logged
- **Data Retention**: Configurable log retention
- **Compliance**: GDPR, SOX compliance ready

## Future Architecture Considerations

### Microservices Evolution
- **Service Decomposition**: Split into smaller services
- **API Gateway**: Centralized API management
- **Service Mesh**: Inter-service communication

### Cloud-Native Features
- **Kubernetes**: Container orchestration
- **Service Discovery**: Dynamic service location
- **Circuit Breaker**: Fault tolerance patterns

### Advanced Monitoring
- **Distributed Tracing**: Request flow tracking
- **Application Performance Monitoring**: Deep insights
- **Predictive Analytics**: Proactive issue detection

### Integration Capabilities
- **Message Queues**: Asynchronous processing
- **Event Streaming**: Real-time data processing
- **API Integration**: Third-party service integration