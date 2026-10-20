
## Giao việc cho Antigravity 2.0 Desktop
Khi gặp các việc tốn thời gian hoặc lặp lại (dựng Map 3D hàng loạt, tạo UI, viết ScriptableObject, cấu hình Prefab), Claude Code chỉ làm phần Core Architecture, sau đó tự động xuất bản giao việc chuẩn xác vào `.agent/tasks/inbox/task_current.md` để Antigravity 2.0 Desktop thực thi song song.

- Xuất bản bằng: `.claude/scripts/delegate-to-anti.sh "Tiêu đề" "Đặc tả: file cần tạo/sửa, yêu cầu Unity 3D/MCP"`
- Script ghi task, mở Antigravity Desktop và copy lệnh kích hoạt vào clipboard để người dùng dán vào chat.
- Quy trình phía Antigravity: `.agent/workflows/auto-worker.md`; task xong nằm ở `.agent/tasks/done/`.
