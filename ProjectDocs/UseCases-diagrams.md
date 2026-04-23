graph TD
    %% Tác nhân
    Player((Player))

    %% Top Level
    Player --> UC_CaiDat[Cài đặt]
    Player --> UC_ChoiGame[Chơi game]

    %% Nhóm Cài đặt
    subgraph "Cài đặt"
        UC_CaiDat
        UC_AmThanh[Âm thanh]
        UC_NhacNen[Nhạc nền]
        UC_CaiDat -. Extend .-> UC_AmThanh
        UC_CaiDat -. Extend .-> UC_NhacNen
    end

    %% Nhóm Chơi game
    subgraph "Chơi Game"
        UC_MoDat[Mở Đất]
        UC_TrongCay[Trồng cây]
        UC_CuaHang[Cửa Hàng]
        UC_ChamSoc[Chăm sóc cây]
        UC_XayDung[Xây dựng]
        UC_NhiemVu[Nhiệm vụ]
        UC_XuatHang[Xuất Hàng]

        Player --> UC_MoDat
        Player --> UC_TrongCay
        Player --> UC_CuaHang
        Player --> UC_ChamSoc
        Player --> UC_XayDung
        Player --> UC_NhiemVu
        Player --> UC_XuatHang

        %% Logic Mở đất
        UC_KeoThaBlock[Kéo thả Block vào lưới]
        UC_NhanTaiNguyen[Nhận tài nguyên]
        UC_MoDat -. Include .-> UC_KeoThaBlock
        UC_NhanTaiNguyen -. Extend .-> UC_KeoThaBlock

        %% Logic Xuất hàng (Minigame Tetris)
        UC_DieuKhien[Điều khiển khối Tetris]
        UC_NhanThuongDH[Nhận thưởng Đơn hàng]
        UC_NhanVang[Nhận Vàng]
        
        UC_XuatHang -. Include .-> UC_DieuKhien
        UC_NhanThuongDH -. Extend .-> UC_DieuKhien
        UC_DieuKhien -. Include .-> UC_NhanVang
    end