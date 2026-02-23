---
title: "CRUD Made Easy: Building Dynamic Apps with htmx and ASP.NET Razor Pages"
date: 2025-04-01
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "CRUD-Made-Easy-Building-Dynamic-Apps-with-htmx-and-ASP.NET-Razor-Pages.png"
---

Welcome back, developer! Today, we’re tackling something everyone needs to build at some point: a CRUD (Create, Read, Update, Delete) application. And guess what? We’re going to do it without the JavaScript bloat. That’s right, htmx is here to make your CRUD dreams come true.

## What We’re Building

We’re building a simple To-Do List where you can:

- Add tasks (Create)

- View tasks (Read)

- Edit tasks (Update)

- Delete tasks (Delete)

The twist? We’re going to handle all interactions dynamically without reloading the page.

## Setting Up Your Project

Start by creating a new ASP.NET Razor Pages project:

```bash
dotnet new razor -n htmxCrudDemo
```

Add htmx to your `_Layout.cshtml`:

```html
<script src="https://unpkg.com/htmx.org"></script>
```

## Building the Backend

Let’s set up the backend to handle all our CRUD operations.

### Index.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace htmxCrudDemo.Pages;

public class IndexModel : PageModel
{
    private static readonly List<TaskItem> Tasks = new();
    private static int _idCounter = 1;

    public IActionResult OnGetLoadTasks()
    {
        var tasksHtml = string.Join("", Tasks.Select(t => $"<div id='task-{t.Id}'>" +
            $"<span>{t.Description}</span> " +
            $"<button hx-get='/EditTask?id={t.Id}' hx-target='#task-{t.Id}'>Edit</button> " +
            $"<button hx-delete='/DeleteTask?id={t.Id}' hx-target='#task-{t.Id}'>Delete</button>" +
            "</div>"));
        
        return Content(tasksHtml, "text/html");
    }

    public IActionResult OnPostAddTask(string description)
    {
        var newTask = new TaskItem { Id = _idCounter++, Description = description };
        Tasks.Add(newTask);

        return Content($"<div id='task-{newTask.Id}'>" +
                       $"<span>{newTask.Description}</span> " +
                       $"<button hx-get='/EditTask?id={newTask.Id}' hx-target='#task-{newTask.Id}'>Edit</button> " +
                       $"<button hx-delete='/DeleteTask?id={newTask.Id}' hx-target='#task-{newTask.Id}'>Delete</button>" +
                       "</div>", "text/html");
    }

    public IActionResult OnGetEditTask(int id)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return NotFound();

        var editForm = $"<form hx-put='/UpdateTask' hx-target='#task-{task.Id}'>" +
                       $"<input type='hidden' name='Id' value='{task.Id}' />" +
                       $"<input type='text' name='Description' value='{task.Description}' />" +
                       "<button type='submit'>Save</button>" +
                       "</form>";
        return Content(editForm, "text/html");
    }

    public IActionResult OnPutUpdateTask(int id, string description)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return NotFound();

        task.Description = description;
        return Content($"<div id='task-{task.Id}'>" +
                       $"<span>{task.Description}</span> " +
                       $"<button hx-get='/EditTask?id={task.Id}' hx-target='#task-{task.Id}'>Edit</button> " +
                       $"<button hx-delete='/DeleteTask?id={task.Id}' hx-target='#task-{task.Id}'>Delete</button>" +
                       "</div>", "text/html");
    }

    public IActionResult OnDeleteDeleteTask(int id)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == id);
        if (task != null) Tasks.Remove(task);

        return Content("");
    }

    private class TaskItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }
}
```

## Building the Frontend

Now, let’s put together the UI that interacts with our backend.

### Index.cshtml

```html
@page
@model IndexModel

<!DOCTYPE html>
<html>
<head>
    <title>htmx CRUD Demo</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Task List</h1>

    <div>
        <form hx-post="/Index?handler=AddTask" hx-target="#task-list">
            <input type="text" name="description" placeholder="New Task" required>
            <button type="submit">Add Task</button>
        </form>
    </div>

    <div id="task-list" hx-get="/Index?handler=LoadTasks" hx-trigger="load"></div>
</body>
</html>
```

## What’s Happening Here

- **Creating Tasks:** The form at the top sends a POST request to `/AddTask` and injects the new task directly into the list without refreshing the page.

- **Reading Tasks:** The task list is loaded from the server when the page loads thanks to `hx-get` and `hx-trigger="load"`.

- **Updating Tasks:** Clicking an Edit button replaces the task’s HTML with a form that lets you update the description. Submitting the form sends a PUT request to update the task.

- **Deleting Tasks:** Clicking a Delete button sends a DELETE request to the server, and the task is removed from the UI instantly.

## Why htmx Wins Here

- It’s all server-driven, so you can stick with your familiar C# and Razor Pages.

- You avoid JavaScript-heavy frameworks but still get all the interactivity you want.

- Your UI code is simple and straightforward. No fancy front-end frameworks required.

We’ve just scratched the surface of what’s possible with htmx and Razor Pages. Next time, we’ll explore more advanced topics and put all this together to build a more feature-rich application.
