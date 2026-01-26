// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
window.addEventListener('scroll', function () {
    const navbar = document.querySelector('.navbar');
    if (window.scrollY > 50) {
        navbar.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.1)';
        navbar.style.padding = '0.5rem 0';
    } else {
        navbar.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.05)';
        navbar.style.padding = '1rem 0';
    }
});

function smartBack() {
    const referrer = document.referrer;
    const currentHost = window.location.hostname;

    console.log('Referrer:', referrer); // Debug
    console.log('Current host:', currentHost); // Debug

    // Kiểm tra referrer có tồn tại và cùng domain
    if (referrer && referrer.includes(currentHost)) {

        // Trường hợp 1: Từ Wishlist
        if (referrer.toLowerCase().includes('/wishlist')) {
            console.log('Returning to Wishlist');
            window.location.href = '/Wishlist/Index';
            return;
        }

        // Trường hợp 2: Từ Shop/Product listing
        if (referrer.toLowerCase().includes('/shop') ||
            referrer.toLowerCase().includes('/product')) {
            console.log('Returning to Shop');
            window.location.href = '/Shop/Index';
            return;
        }

        // Trường hợp 3: Có referrer hợp lệ khác - dùng history.back()
        console.log('Using history.back()');
        window.history.back();

    } else {
        // Không có referrer hoặc từ external - về trang chủ
        console.log('No valid referrer, going to home');
        window.location.href = '/Home/Index';
    }
}

// Tự động phát hiện và cập nhật text/icon của nút Back
function updateBackButton() {
    const backBtn = document.getElementById('smartBackBtn');
    const backText = document.getElementById('backButtonText');
    const backIcon = document.getElementById('backButtonIcon');

    if (!backBtn) return;

    const referrer = document.referrer;

    if (referrer) {
        if (referrer.toLowerCase().includes('/wishlist')) {
            backText.textContent = 'Quay lại Wishlist';
            backIcon.className = 'bi bi-heart-fill';
        } else if (referrer.toLowerCase().includes('/shop') ||
            referrer.toLowerCase().includes('/product')) {
            backText.textContent = 'Quay lại Sản phẩm';
            backIcon.className = 'bi bi-shop';
        } else {
            backText.textContent = 'Quay lại';
            backIcon.className = 'bi bi-arrow-left';
        }
    }
}

// Chạy khi trang load xong
document.addEventListener('DOMContentLoaded', function () {
    updateBackButton();
});
