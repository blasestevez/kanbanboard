# Trellochocero — API Specs

Base URL: `https://localhost:5001/api`

Authentication: Bearer JWT token in `Authorization` header.

---

## Auth Endpoints
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/google`
- `POST /api/auth/github`
- `GET /api/users/me`

---

## Workspaces Endpoints (Fase 2)
- `GET /api/workspaces`
- `POST /api/workspaces`
- `GET /api/workspaces/{id}`
- `PUT /api/workspaces/{id}`
- `DELETE /api/workspaces/{id}`
- `POST /api/workspaces/{id}/members`
- `PUT /api/workspaces/{id}/members/{userId}`
- `DELETE /api/workspaces/{id}/members/{userId}`

---

## Boards & Lists Endpoints (Fase 3)
- `GET /api/workspaces/{workspaceId}/boards`
- `POST /api/workspaces/{workspaceId}/boards`
- `GET /api/boards/{id}`
- `PUT /api/boards/{id}`
- `DELETE /api/boards/{id}`
- `POST /api/boards/{boardId}/lists`
- `PUT /api/boards/{boardId}/lists/reorder`
- `PUT /api/lists/{id}`
- `DELETE /api/lists/{id}`

---

## Cards Core & Features Endpoints (Fase 4)

### POST /api/lists/{listId}/cards
Crea una nueva tarjeta en una lista.

**Request Body:**
```json
{
  "title": "Configurar Auth con JWT",
  "description": "Implementar endpoints de login y registro",
  "coverColor": "#0079bf"
}
```

**Response 201:** CardDetailResponse

---

### GET /api/cards/{id}
Obtiene el detalle completo de una tarjeta: etiquetas, checklists e items, comentarios, adjuntos, miembros asignados.

**Response 200:**
```json
{
  "id": "guid",
  "listId": "guid",
  "boardId": "guid",
  "title": "Configurar Auth con JWT",
  "description": "Implementar endpoints de login y registro",
  "position": 0,
  "dueDate": "2026-09-10T00:00:00Z",
  "isComplete": false,
  "coverColor": "#0079bf",
  "coverImageUrl": null,
  "createdAt": "2026-09-02T20:00:00Z",
  "members": [
    {
      "userId": "guid",
      "fullName": "Alice Dev",
      "email": "alice@example.com",
      "avatarUrl": null
    }
  ],
  "labels": [
    {
      "id": "guid",
      "name": "Backend",
      "color": "#61bd4f"
    }
  ],
  "checklists": [
    {
      "id": "guid",
      "title": "Tareas Pendientes",
      "position": 0,
      "items": [
        {
          "id": "guid",
          "text": "Crear DTOs",
          "isChecked": true,
          "position": 0
        },
        {
          "id": "guid",
          "text": "Generar JWT",
          "isChecked": false,
          "position": 1
        }
      ]
    }
  ],
  "comments": [
    {
      "id": "guid",
      "authorId": "guid",
      "authorName": "Bob Tester",
      "authorAvatarUrl": null,
      "text": "Los tests ya quedaron listos",
      "createdAt": "2026-09-02T20:10:00Z",
      "updatedAt": null
    }
  ],
  "attachments": [
    {
      "id": "guid",
      "fileName": "diagrama.png",
      "fileUrl": "/uploads/cards/guid/diagrama.png",
      "contentType": "image/png",
      "fileSizeBytes": 1048576,
      "createdAt": "2026-09-02T20:15:00Z"
    }
  ]
}
```

---

### PUT /api/cards/{id}
Actualiza datos básicos de la card: título, descripción, due date, isComplete, coverColor, coverImageUrl.

**Request Body:**
```json
{
  "title": "Configurar Auth con JWT & OAuth",
  "description": "Se agregó Google y GitHub",
  "dueDate": "2026-09-12T00:00:00Z",
  "isComplete": true,
  "coverColor": "#519839",
  "coverImageUrl": null
}
```

**Response 200:** CardDetailResponse

---

### PUT /api/cards/{id}/move
Mueve una tarjeta entre listas o reordena su posición (Drag & Drop de cards).

**Request Body:**
```json
{
  "targetListId": "guid-destination-list",
  "newPosition": 2
}
```

**Response 204:** No Content

---

### DELETE /api/cards/{id}
Elimina una tarjeta y todos sus datos relacionados.

**Response 204:** No Content

---

### Labels Endpoints

- `GET /api/boards/{boardId}/labels` -> Retorna etiquetas del tablero.
- `POST /api/boards/{boardId}/labels` -> Crea etiqueta (`name`, `color`).
- `POST /api/cards/{cardId}/labels/{labelId}` -> Asigna etiqueta a tarjeta.
- `DELETE /api/cards/{cardId}/labels/{labelId}` -> Remueve etiqueta de tarjeta.

---

### Checklists Endpoints

- `POST /api/cards/{cardId}/checklists` -> Crea checklist (`title`).
- `DELETE /api/checklists/{id}` -> Elimina checklist.
- `POST /api/checklists/{checklistId}/items` -> Crea item (`text`).
- `PUT /api/checklist-items/{id}` -> Actualiza item (`text`, `isChecked`).
- `DELETE /api/checklist-items/{id}` -> Elimina item.

---

### Comments Endpoints

- `POST /api/cards/{cardId}/comments` -> Agrega comentario (`text`).
- `PUT /api/comments/{id}` -> Edita comentario (`text`).
- `DELETE /api/comments/{id}` -> Elimina comentario.

---

### Attachments Endpoints

- `POST /api/cards/{cardId}/attachments` (Multipart/form-data con archivo). Guarda localmente en disco.
- `DELETE /api/attachments/{id}` -> Elimina archivo y registro.

---

### Card Members Endpoints

- `POST /api/cards/{cardId}/members/{userId}` -> Asigna usuario a la tarjeta.
- `DELETE /api/cards/{cardId}/members/{userId}` -> Remueve usuario de la tarjeta.

---

## DTOs (C# Backend)

```csharp
public record CreateCardRequest(string Title, string? Description, string? CoverColor);
public record UpdateCardRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    bool IsComplete,
    string? CoverColor,
    string? CoverImageUrl);

public record MoveCardRequest(Guid TargetListId, int NewPosition);

public record CardDetailResponse(
    Guid Id,
    Guid ListId,
    Guid BoardId,
    string Title,
    string? Description,
    int Position,
    DateTime? DueDate,
    bool IsComplete,
    string? CoverColor,
    string? CoverImageUrl,
    DateTime CreatedAt,
    List<CardMemberResponse> Members,
    List<LabelResponse> Labels,
    List<ChecklistResponse> Checklists,
    List<CommentResponse> Comments,
    List<AttachmentResponse> Attachments);

public record CardMemberResponse(Guid UserId, string FullName, string Email, string? AvatarUrl);
public record LabelResponse(Guid Id, string Name, string Color);
public record ChecklistResponse(Guid Id, string Title, int Position, List<ChecklistItemResponse> Items);
public record ChecklistItemResponse(Guid Id, string Text, bool IsChecked, int Position);
public record CommentResponse(Guid Id, Guid AuthorId, string AuthorName, string? AuthorAvatarUrl, string Text, DateTime CreatedAt, DateTime? UpdatedAt);
public record AttachmentResponse(Guid Id, string FileName, string FileUrl, string ContentType, long FileSizeBytes, DateTime CreatedAt);

public record CreateLabelRequest(string Name, string Color);
public record CreateChecklistRequest(string Title);
public record CreateChecklistItemRequest(string Text);
public record UpdateChecklistItemRequest(string Text, bool IsChecked);
public record CreateCommentRequest(string Text);
public record UpdateCommentRequest(string Text);
```

---

## Real-Time SignalR Hub (Fase 5 — Real-time Updates)

**Hub Endpoint**: `/hubs/board`  
**Protocol**: WebSockets / Server-Sent Events / Long Polling (fallback)  
**Auth**: Bearer token via `access_token` query parameter or `Authorization` header.

### Client-to-Server Methods
- `JoinBoard(string boardId)`: Agrega la conexión al grupo `board-{boardId}` para recibir eventos en tiempo real de ese tablero.
- `LeaveBoard(string boardId)`: Remueve la conexión del grupo `board-{boardId}` al abandonar el tablero.

### Server-to-Client Broadcast Events (emitidos al grupo `board-{boardId}`)

| Evento | Payload | Descripción |
|:-------|:--------|:------------|
| `BoardUpdated` | `{ title: string, backgroundColor?: string, backgroundImageUrl?: string, isClosed: boolean }` | Notifica cambios de metadatos o apariencia del tablero |
| `ListCreated` | `BoardListResponse` | Notifica la creación de una nueva lista en el tablero |
| `ListUpdated` | `BoardListResponse` | Notifica cambios en el título o estado de una lista |
| `ListDeleted` | `Guid listId` | Notifica la eliminación de una lista |
| `ListsReordered` | `List<Guid> listIds` | Notifica el nuevo orden horizontal de las listas |
| `CardCreated` | `CardDetailResponse` | Notifica la creación de una nueva tarjeta |
| `CardUpdated` | `CardDetailResponse` | Notifica la edición de una tarjeta (título, fechas, covers) |
| `CardMoved` | `{ cardId: Guid, sourceListId: Guid, targetListId: Guid, newPosition: int }` | Notifica movimiento de tarjeta dentro de lista o entre listas |
| `CardDeleted` | `{ cardId: Guid, listId: Guid }` | Notifica la eliminación de una tarjeta |

