# Workspace Layouts

## Standard page stack

```
[ ws-page ]
  └─ ws-layout
       ├─ ws-atf (+ ws-atf--band optional)
       │    └─ app-page-workspace-hero
       └─ ws-workspace / ws-grid
```

## Layout variants

### Main + sticky rail (Profile, Subscription active)

```html
<div class="ws-grid ws-grid--main-rail">
  <div class="ws-panel-pad">…main…</div>
  <aside class="ws-aside--sticky">…rail…</aside>
</div>
```

Collapses to single column at `960px`.

### Action + reference (Subscription idle)

```html
<main class="ws-grid ws-grid--action-main">
  <section class="ws-panel-pad">…form…</section>
  <aside class="ws-panel-border-start">…plans…</aside>
</main>
```

### Master / detail (Tickets)

```html
<div class="ws-master-detail ws-master-detail--split">
  <section>…queue…</section>
  <aside>…conversation…</aside>
</div>
```

Viewport height and internal scroll are page-specific extensions in `tickets.component.css`.

### Filter toolbar

```html
<section class="ws-toolbar">
  <div class="filter-pills">
    <button class="filter-pill is-active">…</button>
  </div>
</section>
```

## Empty placeholder

```html
<div class="ws-placeholder-panel">
  <div class="ws-placeholder-panel-icon">…</div>
  <h2 class="ws-placeholder-panel-title">…</h2>
  <p class="ws-placeholder-panel-body muted">…</p>
</div>
```
