namespace FileTogether.Core.Protocol;

public enum Command : byte
{
    // Client -> Server requests
    LIST = 1,      // Yêu cầu danh sách file
    UPLOAD = 2,    // Gửi file lên
    DOWNLOAD = 3,  // Tải file về
    DELETE = 4,    // Xóa file
        
    // Server -> Client responses
    OK = 10,       // Thành công
    ERROR = 11,    // Lỗi
    ITEM_LIST = 12, // Trả về danh sách file
    UNAUTHORIZED = 13,
    
    //Authentication
    LOGIN = 20,          // Client gửi username/password
    LOGIN_RESPONSE = 21, // Server trả kết quả login
    LOGOUT = 22,         // Client đăng xuất
    REGISTER = 23,           
    REGISTER_RESPONSE = 24,
    
    // Directory operations
    CREATE_DIR = 30,    // Tạo thư mục
    DELETE_DIR = 31,    // Xóa thư mục
    CHANGE_DIR = 32,    // Chuyển thư mục
    GET_CURRENT_DIR = 33 // Lấy thư mục hiện tại
}