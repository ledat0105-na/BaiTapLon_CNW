/**
 * Script để xử lý chọn tỉnh/quận/huyện/phường/xã
 * Sử dụng API từ provinces.open-api.vn
 */

(function() {
    'use strict';

    const AddressSelector = {
        init: function() {
            this.loadProvinces();
            this.bindEvents();
        },

        bindEvents: function() {
            // Khi chọn tỉnh/thành phố
            const provinceSelect = document.getElementById('ProvinceCode');
            if (provinceSelect) {
                provinceSelect.addEventListener('change', () => {
                    const provinceCode = provinceSelect.value;
                    if (provinceCode) {
                        this.loadDistricts(provinceCode);
                        // Reset quận/huyện và phường/xã
                        this.resetSelect('DistrictCode');
                        this.resetSelect('WardCode');
                    } else {
                        this.resetSelect('DistrictCode');
                        this.resetSelect('WardCode');
                    }
                });
            }

            // Khi chọn quận/huyện
            const districtSelect = document.getElementById('DistrictCode');
            if (districtSelect) {
                districtSelect.addEventListener('change', () => {
                    const districtCode = districtSelect.value;
                    if (districtCode) {
                        this.loadWards(districtCode);
                        // Reset phường/xã
                        this.resetSelect('WardCode');
                    } else {
                        this.resetSelect('WardCode');
                    }
                });
            }
        },

        loadProvinces: async function() {
            const select = document.getElementById('ProvinceCode');
            if (!select) return;

            try {
                select.disabled = true;
                select.innerHTML = '<option value="">Đang tải...</option>';

                const response = await fetch('/api/AddressApi/provinces');
                if (!response.ok) {
                    throw new Error('Không thể tải danh sách tỉnh/thành phố');
                }

                const provinces = await response.json();
                select.innerHTML = '<option value="">-- Chọn tỉnh/thành phố --</option>';

                provinces.forEach(province => {
                    const option = document.createElement('option');
                    option.value = province.code;
                    option.textContent = province.name;
                    select.appendChild(option);
                });

                select.disabled = false;
            } catch (error) {
                console.error('Error loading provinces:', error);
                console.error('Error details:', error.message, error.stack);
                select.innerHTML = '<option value="">Lỗi khi tải danh sách (kiểm tra Console)</option>';
                select.disabled = false;
                
                // Thử reload sau 2 giây
                setTimeout(() => {
                    if (select.innerHTML.includes('Lỗi')) {
                        this.loadProvinces();
                    }
                }, 2000);
            }
        },

        loadDistricts: async function(provinceCode) {
            const select = document.getElementById('DistrictCode');
            if (!select) return;

            try {
                select.disabled = true;
                select.innerHTML = '<option value="">Đang tải...</option>';

                const response = await fetch(`/api/AddressApi/districts/${provinceCode}`);
                if (!response.ok) {
                    throw new Error('Không thể tải danh sách quận/huyện');
                }

                const districts = await response.json();
                select.innerHTML = '<option value="">-- Chọn quận/huyện --</option>';

                districts.forEach(district => {
                    const option = document.createElement('option');
                    option.value = district.code;
                    option.textContent = district.name;
                    select.appendChild(option);
                });

                select.disabled = false;
            } catch (error) {
                console.error('Error loading districts:', error);
                select.innerHTML = '<option value="">Lỗi khi tải danh sách</option>';
                select.disabled = false;
            }
        },

        loadWards: async function(districtCode) {
            const select = document.getElementById('WardCode');
            if (!select) return;

            try {
                select.disabled = true;
                select.innerHTML = '<option value="">Đang tải...</option>';

                const response = await fetch(`/api/AddressApi/wards/${districtCode}`);
                if (!response.ok) {
                    throw new Error('Không thể tải danh sách phường/xã');
                }

                const wards = await response.json();
                select.innerHTML = '<option value="">-- Chọn phường/xã --</option>';

                wards.forEach(ward => {
                    const option = document.createElement('option');
                    option.value = ward.code;
                    option.textContent = ward.name;
                    select.appendChild(option);
                });

                select.disabled = false;
            } catch (error) {
                console.error('Error loading wards:', error);
                select.innerHTML = '<option value="">Lỗi khi tải danh sách</option>';
                select.disabled = false;
            }
        },

        resetSelect: function(selectId) {
            const select = document.getElementById(selectId);
            if (select) {
                select.innerHTML = '<option value="">-- Chọn --</option>';
                select.disabled = true;
            }
        },

        // Hàm để cập nhật địa chỉ đầy đủ vào trường Address
        updateFullAddress: function() {
            const addressInput = document.getElementById('Address');
            if (!addressInput) return;

            const provinceSelect = document.getElementById('ProvinceCode');
            const districtSelect = document.getElementById('DistrictCode');
            const wardSelect = document.getElementById('WardCode');
            const streetInput = document.getElementById('Street') || document.getElementById('AddressDetail');

            const parts = [];
            
            // Số nhà, tên đường
            if (streetInput && streetInput.value) {
                parts.push(streetInput.value.trim());
            }
            
            // Phường/Xã
            if (wardSelect && wardSelect.selectedIndex > 0) {
                parts.push(wardSelect.options[wardSelect.selectedIndex].text);
            }
            
            // Quận/Huyện
            if (districtSelect && districtSelect.selectedIndex > 0) {
                parts.push(districtSelect.options[districtSelect.selectedIndex].text);
            }
            
            // Tỉnh/Thành phố
            if (provinceSelect && provinceSelect.selectedIndex > 0) {
                parts.push(provinceSelect.options[provinceSelect.selectedIndex].text);
            }

            const fullAddress = parts.filter(p => p).join(', ');
            if (fullAddress && addressInput) {
                addressInput.value = fullAddress;
            }
        }
    };

    // Khởi tạo khi DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            AddressSelector.init();
            
            // Thêm event listener để cập nhật địa chỉ đầy đủ
            ['ProvinceCode', 'DistrictCode', 'WardCode'].forEach(id => {
                const select = document.getElementById(id);
                if (select) {
                    select.addEventListener('change', () => {
                        setTimeout(() => AddressSelector.updateFullAddress(), 100);
                    });
                }
            });

            const streetInput = document.getElementById('Street') || document.getElementById('AddressDetail');
            if (streetInput) {
                streetInput.addEventListener('blur', () => {
                    AddressSelector.updateFullAddress();
                });
                streetInput.addEventListener('input', () => {
                    AddressSelector.updateFullAddress();
                });
            }

            // Cập nhật địa chỉ trước khi submit form
            const forms = document.querySelectorAll('form');
            forms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    AddressSelector.updateFullAddress();
                });
            });
        });
    } else {
        AddressSelector.init();
        
        // Cập nhật địa chỉ trước khi submit form
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                AddressSelector.updateFullAddress();
            });
        });
    }

    // Export để có thể sử dụng từ bên ngoài
    window.AddressSelector = AddressSelector;
})();

